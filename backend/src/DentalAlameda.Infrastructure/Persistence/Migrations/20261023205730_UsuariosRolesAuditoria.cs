using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DentalAlameda.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UsuariosRolesAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: true); // usuarios existentes quedan activos

            migrationBuilder.AddColumn<DateTime>(
                name: "CreadoEn",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<bool>(
                name: "DebeCambiarPassword",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OdontologoId",
                table: "usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoIngresoEn",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Anulado",
                table: "registros_clinicos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnuladoEn",
                table: "registros_clinicos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnuladoPorNombre",
                table: "registros_clinicos",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutorNombre",
                table: "registros_clinicos",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AutorUsuarioId",
                table: "registros_clinicos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "registros_clinicos",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AutorizaDatosEn",
                table: "pacientes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "auditoria_clinica",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    UsuarioNombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UsuarioRol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Accion = table.Column<int>(type: "integer", nullable: false),
                    PacienteId = table.Column<int>(type: "integer", nullable: false),
                    Detalle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria_clinica", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_OdontologoId",
                table: "usuarios",
                column: "OdontologoId");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_clinica_Fecha",
                table: "auditoria_clinica",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_clinica_PacienteId_Fecha",
                table: "auditoria_clinica",
                columns: new[] { "PacienteId", "Fecha" });

            migrationBuilder.AddForeignKey(
                name: "FK_usuarios_odontologos_OdontologoId",
                table: "usuarios",
                column: "OdontologoId",
                principalTable: "odontologos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_usuarios_odontologos_OdontologoId",
                table: "usuarios");

            migrationBuilder.DropTable(
                name: "auditoria_clinica");

            migrationBuilder.DropIndex(
                name: "IX_usuarios_OdontologoId",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "CreadoEn",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "DebeCambiarPassword",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "OdontologoId",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "UltimoIngresoEn",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "Anulado",
                table: "registros_clinicos");

            migrationBuilder.DropColumn(
                name: "AnuladoEn",
                table: "registros_clinicos");

            migrationBuilder.DropColumn(
                name: "AnuladoPorNombre",
                table: "registros_clinicos");

            migrationBuilder.DropColumn(
                name: "AutorNombre",
                table: "registros_clinicos");

            migrationBuilder.DropColumn(
                name: "AutorUsuarioId",
                table: "registros_clinicos");

            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                table: "registros_clinicos");

            migrationBuilder.DropColumn(
                name: "AutorizaDatosEn",
                table: "pacientes");
        }
    }
}
