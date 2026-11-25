# VetCare Web – Arquitectura y decisiones de diseño

Proyecto integrador de **Programación para Web**. Sistema de gestión veterinaria
en **Blazor Web App (.NET 10)** con **Entity Framework Core 10 + SQL Server**.

---

## 1. Cómo arrancar desde cero

1. `dotnet ef database drop --force` (solo si ya existía una base anterior).
2. Borra la carpeta `Migrations/` (si existe de una versión anterior).
3. `dotnet ef migrations add InitialCreate`
4. `dotnet watch` (o F5 en Visual Studio).

Al arrancar, `Data/DbInitializer.cs` **aplica las migraciones automáticamente**
y crea los datos iniciales. No hace falta `dotnet ef database update`.

### Cuentas creadas al iniciar

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador | admin@vetcare.mx | Admin123* |
| Veterinario | carlos.ruiz@vetcare.mx · ana.martinez@vetcare.mx · luis.gomez@vetcare.mx | Vet123* |
| Cliente | roberto.gomez@example.com · maria.fernandez@example.com · juan.perez@example.com · laura.sanchez@example.com · carmen.torres@example.com | Cliente123* |

- El administrador se crea **siempre** que no exista ninguno (correo y contraseña en `appsettings.json` → `VetCare:Admin`).
- Los datos de demostración (3 veterinarios, 5 clientes, 6 mascotas, 10 citas alrededor de la fecha actual, 4 consultas y 5 vacunas) se cargan si `VetCare:SembrarDatosDemo` es `true` y aún no hay veterinarios. Pon `false` para iniciar solo con el administrador.
- En modo Development, la página de inicio de sesión muestra estas cuentas de prueba.

---

## 2. Estructura del proyecto

```
Components/
  Layout/        MainLayout (navbar + footer), NavMenu (menú por rol)
  Pages/         Páginas con @page
    Cuenta/      Login, Registro, AccesoDenegado, MiPerfil
    Mascotas/    Lista, Formulario, Expediente, ResumenConsulta (imprimible)
    Citas/       NuevaCita, MisCitas, Agenda, Consulta
    Propietarios/ Lista, Formulario
    Admin/       Personal (veterinarios y administradores)
  Paneles/       PanelCliente, PanelPersonal (dashboard por rol)
  Shared/        Hero, Módulos, Métricas, TablaCitas, EstadoCitaBadge...
Data/            VetCareDbContext, DbInitializer (migraciones + semilla)
Models/          Entidades, modelos de formulario, Textos, ResultadoOperacion
Seguridad/       Cookie de sesión, roles, usuario actual, endpoints de cuenta
Services/        Lógica de negocio con permisos (I*Service + implementación)
```

**Capas:** las páginas solo llaman servicios. Los servicios validan las reglas
de negocio **y los permisos**. Las páginas no consultan la BD directamente.

---

## 3. Decisiones importantes

### 3.1 Autenticación propia con cookie (sin ASP.NET Identity)
- Se conserva la tabla `usuarios` del modelo de datos (`email`, `password_hash`, `rol`).
  ASP.NET Identity habría reemplazado el esquema por sus propias tablas.
- Las contraseñas se guardan con `PasswordHasher<Usuario>` (PBKDF2 con sal), nunca en texto plano.
- La sesión es una cookie `HttpOnly` con los claims: id, nombre, correo, rol y el id de propietario.
- **Login y Registro son páginas estáticas (SSR)**, porque solo una petición HTTP normal
  puede escribir la cookie; una página interactiva no puede (usa WebSocket).
- Cerrar sesión es un `POST /cuenta/logout` con token antiforgery.
- **Validación de la sesión:** cada minuto, `SesionCookie.ValidarAsync` revisa en la BD
  que la cuenta siga existiendo y activa. Si el administrador la desactiva, la sesión se cierra sola.

### 3.2 Roles y permisos en dos niveles
1. **Páginas:** `@attribute [Authorize(Roles = ...)]`. Sin sesión, el usuario va al login; con un rol incorrecto, a "Acceso denegado".
2. **Servicios:** cada método vuelve a comprobar el rol y la propiedad del dato
   mediante `ISesionService`. Por ejemplo, un cliente no puede ver ni editar la mascota
   de otro aunque escriba la URL a mano. Esta es la protección real.

| Función | Cliente | Veterinario | Administrador |
|---|:-:|:-:|:-:|
| Registrarse / Mi perfil | ✔ | ✔ (perfil) | ✔ (perfil) |
| Mascotas | solo las suyas (CRUD) | todas (lectura) | todas (CRUD) |
| Expediente y carnet | ver los suyos | ver todos + registrar vacunas | ver todos + corregir |
| Agendar cita | para sus mascotas | para cualquier mascota | para cualquier mascota |
| Mis citas / cancelar | ✔ (programadas y futuras) | – | – |
| Agenda diaria | – | ✔ (atiende las suyas) | ✔ (todas) |
| Atender consulta (RF-06) | – | ✔ solo sus citas | – |
| Propietarios | – | lectura | CRUD + activar/desactivar |
| Personal (vets/admins) | – | – | ✔ |
| Resumen imprimible (RF-10) | sus consultas | ✔ | ✔ |

### 3.3 Flujo del cliente (sin intervención manual)
`Crear cuenta` → la sesión inicia sola → `Registrar mascota` → aviso "¿Agendar cita?" →
`Agendar cita` (veterinario, fecha y **solo horarios libres**) → `Mis citas` →
después de la consulta: `Expediente`, `Carnet` y `Resumen imprimible`.

### 3.4 Agenda y disponibilidad (RF-04 / RF-05)
- La clínica atiende de lunes a sábado, de 09:00 a 18:00, en **bloques de 30 minutos**
  (configurable en `Services/ReglasNegocio.cs` → `HorarioClinica`).
- La pantalla solo ofrece bloques libres del veterinario elegido.
- **Doble protección contra empalmes:** el servicio valida y, además, un **índice único filtrado**
  en la BD (`UX_citas_veterinario_horario`: veterinario + fecha_hora, excluyendo canceladas)
  impide que dos personas reserven el mismo bloque al mismo tiempo.
- Ciclo de estados (`ReglasCita`):
  `Programada → En atención → Atendida`, y `Programada / En atención → Cancelada`.
  "Atendida" **solo** se alcanza al registrar la consulta, así toda cita atendida tiene su expediente.

### 3.5 Consulta y expediente (RF-06 / RF-07)
- El veterinario pulsa *Atender* (la cita pasa a "En atención"), captura peso, síntomas,
  diagnóstico, tratamiento e indicaciones, y guarda. La cita queda "Atendida" y el peso
  medido se vuelve el peso actual de la mascota.
- Los recordatorios de vacunación toman la **última** aplicación de cada producto
  y avisan de dosis vencidas o que vencen en los próximos 30 días.

### 3.6 Bajas
- **Personal:** no se elimina, **se desactiva**, porque sus citas y consultas deben conservar la referencia.
  No se puede desactivar al último administrador ni a uno mismo, ni a un veterinario con citas pendientes.
- **Mascotas:** el cliente solo puede eliminar mascotas **sin expediente clínico**.
  El administrador puede eliminarlas, y entonces se borran en cascada.
- **Propietarios:** el administrador puede desactivarlos (no inician sesión) o eliminarlos (borra todo).

### 3.7 Datos iniciales en tiempo de ejecución (no `HasData`)
`HasData` exige valores fijos, así que las contraseñas no podrían cifrarse con sal aleatoria
ni las citas de ejemplo quedarían en fechas cercanas a "hoy". `DbInitializer` crea los datos
al arrancar y es **idempotente**: no duplica nada.

---

## 4. Cambios al modelo de datos original

| Tabla | Cambio | Motivo |
|---|---|---|
| usuarios | + `activo` (bit) | Desactivar cuentas sin perder el historial. |
| historial_clinico | + `peso_kg` decimal(5,2), + `indicaciones` | El RF-06 pide peso actual e indicaciones médicas. |
| historial_clinico | `cita_id` **UNIQUE** | Cada cita genera como máximo una consulta (1:1 opcional, como sugiere el propio documento de modelado). |
| citas_medicas | índice único filtrado (veterinario_id, fecha_hora) | El RF-05 pide evitar la duplicidad de horarios. |
| citas_medicas.estado | + valor `EnAtencion` | El RF-05 y el prototipo lo incluyen. |
| vacunaciones | entidad complementaria | El RF-07, ya propuesta en el documento de modelado. |

---

## 5. Pendiente / posibles mejoras
- Recuperar la contraseña por correo (requiere un servicio de envío de correos).
- Reprogramar una cita sin cancelarla.
- Notificaciones o recordatorios automáticos por correo o WhatsApp.
- Reportes del administrador (citas por veterinario, por mes).
