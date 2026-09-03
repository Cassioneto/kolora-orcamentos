namespace Kolora.Orcamentos.Models.Entities;

public class ProdutoMaterial
{
    public Guid ProdutoId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal ConsumoPorUnidade { get; set; }
    public Produto? Produto { get; set; }
    public Material? Material { get; set; }
}