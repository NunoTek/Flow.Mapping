namespace Flow.Mapping.Models;

public class Transcodification
{
    public int? FluxId { get; set; }

    public string ChampDestination { get; set; }
    public string EntiteDestination { get; set; }

    public string ValeurSource { get; set; } // Valeur du flux xml
    public string ValeurCible { get; set; } // Valeur correspondante en base
    public string LibelleCible { get; set; } // Libelle en bdd

}
