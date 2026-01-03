using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EconomIA.Adapters.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SincronizarModelo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orgao_monitorado_identificador_do_orgao_to_orgao_identificador",
                table: "orgao_monitorado");

            migrationBuilder.RenameIndex(
                name: "un_orgao_monitorado_identificador_do_orgao",
                table: "orgao_monitorado",
                newName: "un_orgao_monitorado_IdentificadorDoOrgao");

            migrationBuilder.AddForeignKey(
                name: "fk_orgao_monitorado_IdentificadorDoOrgao_to_orgao_Id",
                table: "orgao_monitorado",
                column: "identificador_do_orgao",
                principalTable: "orgao",
                principalColumn: "identificador",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orgao_monitorado_IdentificadorDoOrgao_to_orgao_Id",
                table: "orgao_monitorado");

            migrationBuilder.RenameIndex(
                name: "un_orgao_monitorado_IdentificadorDoOrgao",
                table: "orgao_monitorado",
                newName: "un_orgao_monitorado_identificador_do_orgao");

            migrationBuilder.AddForeignKey(
                name: "fk_orgao_monitorado_identificador_do_orgao_to_orgao_identificador",
                table: "orgao_monitorado",
                column: "identificador_do_orgao",
                principalTable: "orgao",
                principalColumn: "identificador",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
