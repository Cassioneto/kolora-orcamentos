namespace Kolora.Orcamentos.Models.Entities;

public class SyncState
{
    public int Id { get; set; } = 1;
    public DateTime? UltimoPullCatalogo { get; set; }
    public DateTime? UltimoPullPedidos { get; set; }
    public int VersaoSchemaLocal { get; set; }
}