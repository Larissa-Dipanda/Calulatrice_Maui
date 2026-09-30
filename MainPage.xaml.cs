using System.Globalization;

namespace Calculatrice;

public partial class MainPage : ContentPage
{
    string _saisie = "0";          // nombre affiché / en cours de saisie
    double _operande;              // premier opérande
    string? _operateur;            // opérateur en attente
    string _historique = "";       // ex. "8 ÷ 2 ="
    bool _nouvelleSaisie = true;   // le prochain chiffre remplace l'affichage
    bool _erreur;
    bool _paysage;

    public MainPage()
    {
        InitializeComponent();
        MettreAJour();
    }

    // Orientation : écran à gauche, clavier à droite en paysage
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0 || height <= 0) return;

        bool paysage = width > height;
        if (paysage == _paysage) return;
        _paysage = paysage;

        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        if (paysage)
        {
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.4, GridUnitType.Star)));
            RootGrid.ColumnSpacing = 8;
            Grid.SetRow(AffichageBorder, 0);  Grid.SetColumn(AffichageBorder, 0);
            Grid.SetRow(ClavierGrid, 0);      Grid.SetColumn(ClavierGrid, 1);
        }
        else
        {
            RootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(2, GridUnitType.Star)));
            RootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(5, GridUnitType.Star)));
            RootGrid.ColumnSpacing = 0;
            Grid.SetRow(AffichageBorder, 0);  Grid.SetColumn(AffichageBorder, 0);
            Grid.SetRow(ClavierGrid, 1);      Grid.SetColumn(ClavierGrid, 0);
        }
    }

    // Permet d'aligner le résultat à droite dans le ScrollView horizontal
    void OnScrollSizeChanged(object? sender, EventArgs e)
        => ResultatLabel.MinimumWidthRequest = ResultatScroll.Width;

    //  Saisie
    void OnChiffre(object? sender, EventArgs e)
    {
        if (_erreur) Reinitialiser();
        string chiffre = ((Button)sender!).Text;

        if (_nouvelleSaisie)
        {
            if (_operateur == null) _historique = "";
            _saisie = chiffre;
            _nouvelleSaisie = false;
        }
        else
        {
            if (_saisie.Replace("-", "").Replace(".", "").Length >= 15) return;
            _saisie = _saisie == "0" ? chiffre
                    : _saisie == "-0" ? "-" + chiffre
                    : _saisie + chiffre;
        }
        MettreAJour();
    }

    void OnDecimal(object? sender, EventArgs e)
    {
        if (_erreur) Reinitialiser();

        if (_nouvelleSaisie)
        {
            if (_operateur == null) _historique = "";
            _saisie = "0.";
            _nouvelleSaisie = false;
        }
        else if (!_saisie.Contains('.'))
        {
            _saisie += ".";
        }
        MettreAJour();
    }

    void OnEffacer(object? sender, EventArgs e)
    {
        if (_erreur) { Reinitialiser(); MettreAJour(); return; }
        if (_nouvelleSaisie) return;

        _saisie = _saisie[..^1];
        if (_saisie == "" || _saisie == "-") _saisie = "0";
        MettreAJour();
    }

    void OnToutEffacer(object? sender, EventArgs e)
    {
        Reinitialiser();
        MettreAJour();
    }

    void OnSigne(object? sender, EventArgs e)
    {
        if (_erreur) return;

        if (_nouvelleSaisie && _operateur != null)
        {
            _saisie = "-0";
            _nouvelleSaisie = false;
        }
        else
        {
            _saisie = _saisie.StartsWith('-') ? _saisie[1..] : "-" + _saisie;
        }
        MettreAJour();
    }

    void OnPourcent(object? sender, EventArgs e)
    {
        if (_erreur) return;

        double v = Parse(_saisie);
        // "50 + 10 %" => 10 % de 50 = 5 ; sinon simple division par 100
        v = _operateur != null ? _operande * v / 100 : v / 100;
        _saisie = Format(v);
        _nouvelleSaisie = false;
        MettreAJour();
    }

    // Calcul
    void OnOperateur(object? sender, EventArgs e)
    {
        if (_erreur) return;
        string op = ((Button)sender!).Text;

        if (_operateur != null && !_nouvelleSaisie)
        {
            if (!Calculer()) return;          // enchaînement : 2 + 3 + ...
        }
        else if (_operateur == null)
        {
            _operande = Parse(_saisie);
        }

        _operateur = op;
        _nouvelleSaisie = true;
        MettreAJour();
    }

    void OnEgal(object? sender, EventArgs e)
    {
        if (_erreur || _operateur == null) return;

        string expression = $"{Format(_operande)} {_operateur} {_saisie} =";
        if (Calculer())
        {
            _historique = expression;
            _operateur = null;
            _nouvelleSaisie = true;
        }
        MettreAJour();
    }

    // Retourne false en cas d'erreur (division par zéro, dépassement)
    bool Calculer()
    {
        double b = Parse(_saisie);
        double r;

        switch (_operateur)
        {
            case "+": r = _operande + b; break;
            case "−": r = _operande - b; break;
            case "×": r = _operande * b; break;
            case "÷":
                if (b == 0)
                {
                    _historique = $"{Format(_operande)} ÷ 0 =";
                    Erreur("Division par zéro");
                    return false;
                }
                r = _operande / b;
                break;
            default: return true;
        }

        if (double.IsInfinity(r) || double.IsNaN(r))
        {
            Erreur("Dépassement");
            return false;
        }

        _operande = r;
        _saisie = Format(r);
        _nouvelleSaisie = true;
        return true;
    }

    void Erreur(string message)
    {
        _erreur = true;
        _saisie = message;
        _operateur = null;
        _operande = 0;
        _nouvelleSaisie = true;
        MettreAJour();
    }

    void Reinitialiser()
    {
        _saisie = "0";
        _operande = 0;
        _operateur = null;
        _historique = "";
        _nouvelleSaisie = true;
        _erreur = false;
    }

    // Utilitaires
    static double Parse(string s) => double.Parse(s, CultureInfo.InvariantCulture);

    static string Format(double v) => v.ToString("G12", CultureInfo.InvariantCulture);

    void MettreAJour()
    {
        ResultatLabel.Text = _saisie;
        OperationLabel.Text = _erreur ? _historique
                            : _operateur != null ? $"{Format(_operande)} {_operateur}"
                            : _historique;

        // Fait défiler vers la fin pour les très longs nombres
        _ = ResultatScroll.ScrollToAsync(ResultatScroll.ContentSize.Width, 0, false);
    }
}