using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VETCARE_WEB.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    fecha_registro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "propietarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contacto_emergencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_propietarios", x => x.id);
                    table.ForeignKey(
                        name: "FK_propietarios_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mascotas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    propietario_id = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    especie = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    raza = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    fecha_nacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    sexo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    peso_kg = table.Column<decimal>(type: "decimal(5,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mascotas", x => x.id);
                    table.ForeignKey(
                        name: "FK_mascotas_propietarios_propietario_id",
                        column: x => x.propietario_id,
                        principalTable: "propietarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "citas_medicas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mascota_id = table.Column<int>(type: "int", nullable: false),
                    veterinario_id = table.Column<int>(type: "int", nullable: false),
                    fecha_hora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_citas_medicas", x => x.id);
                    table.ForeignKey(
                        name: "FK_citas_medicas_mascotas_mascota_id",
                        column: x => x.mascota_id,
                        principalTable: "mascotas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_citas_medicas_usuarios_veterinario_id",
                        column: x => x.veterinario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vacunaciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mascota_id = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    lote = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    fecha_aplicacion = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_proxima_dosis = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vacunaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_vacunaciones_mascotas_mascota_id",
                        column: x => x.mascota_id,
                        principalTable: "mascotas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "historial_clinico",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mascota_id = table.Column<int>(type: "int", nullable: false),
                    cita_id = table.Column<int>(type: "int", nullable: false),
                    diagnostico = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tratamiento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sintomas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    peso_kg = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    indicaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fecha_atencion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_clinico", x => x.id);
                    table.ForeignKey(
                        name: "FK_historial_clinico_citas_medicas_cita_id",
                        column: x => x.cita_id,
                        principalTable: "citas_medicas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_historial_clinico_mascotas_mascota_id",
                        column: x => x.mascota_id,
                        principalTable: "mascotas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_citas_medicas_mascota_id",
                table: "citas_medicas",
                column: "mascota_id");

            migrationBuilder.CreateIndex(
                name: "UX_citas_veterinario_horario",
                table: "citas_medicas",
                columns: new[] { "veterinario_id", "fecha_hora" },
                unique: true,
                filter: "[estado] <> 'Cancelada'");

            migrationBuilder.CreateIndex(
                name: "IX_historial_clinico_cita_id",
                table: "historial_clinico",
                column: "cita_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_historial_clinico_mascota_id",
                table: "historial_clinico",
                column: "mascota_id");

            migrationBuilder.CreateIndex(
                name: "IX_mascotas_propietario_id",
                table: "mascotas",
                column: "propietario_id");

            migrationBuilder.CreateIndex(
                name: "IX_propietarios_usuario_id",
                table: "propietarios",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vacunaciones_mascota_id",
                table: "vacunaciones",
                column: "mascota_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historial_clinico");

            migrationBuilder.DropTable(
                name: "vacunaciones");

            migrationBuilder.DropTable(
                name: "citas_medicas");

            migrationBuilder.DropTable(
                name: "mascotas");

            migrationBuilder.DropTable(
                name: "propietarios");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
