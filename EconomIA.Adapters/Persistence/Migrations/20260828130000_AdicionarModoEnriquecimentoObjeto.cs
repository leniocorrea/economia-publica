using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EconomIA.Adapters.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarModoEnriquecimentoObjeto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE public.execucao_carga ALTER COLUMN modo_execucao TYPE VARCHAR(50);");
            migrationBuilder.Sql("ALTER TABLE public.execucao_carga DROP CONSTRAINT IF EXISTS chk_modo_execucao;");
            migrationBuilder.Sql(
                "ALTER TABLE public.execucao_carga ADD CONSTRAINT chk_modo_execucao CHECK (modo_execucao IN ('diaria', 'incremental', 'manual', 'orgaos', 'brasil', 'reconciliacao', 'enriquecimento', 'enriquecimento_objeto'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM public.execucao_carga WHERE modo_execucao = 'enriquecimento_objeto';");
            migrationBuilder.Sql("ALTER TABLE public.execucao_carga DROP CONSTRAINT IF EXISTS chk_modo_execucao;");
            migrationBuilder.Sql(
                "ALTER TABLE public.execucao_carga ADD CONSTRAINT chk_modo_execucao CHECK (modo_execucao IN ('diaria', 'incremental', 'manual', 'orgaos', 'brasil', 'reconciliacao', 'enriquecimento'));");
            migrationBuilder.Sql("ALTER TABLE public.execucao_carga ALTER COLUMN modo_execucao TYPE VARCHAR(20);");
        }
    }
}
