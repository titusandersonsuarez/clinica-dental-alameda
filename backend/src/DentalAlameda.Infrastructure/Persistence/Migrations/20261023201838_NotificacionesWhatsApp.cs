using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DentalAlameda.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotificacionesWhatsApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AceptaWhatsApp",
                table: "pacientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CitaId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Telefono = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Intentos = table.Column<int>(type: "integer", nullable: false),
                    UltimoError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IdProveedor = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProximoIntentoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EnviadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notificaciones_citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_CitaId",
                table: "notificaciones",
                column: "CitaId");

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_Estado_ProximoIntentoEn",
                table: "notificaciones",
                columns: new[] { "Estado", "ProximoIntentoEn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notificaciones");

            migrationBuilder.DropColumn(
                name: "AceptaWhatsApp",
                table: "pacientes");
        }
    }
}
