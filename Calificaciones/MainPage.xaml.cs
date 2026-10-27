using System.Globalization;

namespace Calificaciones;

// Dibuja el anillo de progreso de la nota final
public class RingDrawable : IDrawable
{
    public float Progress { get; set; }
    public Color ArcColor { get; set; } = Colors.White;

    public void Draw(ICanvas canvas, RectF r)
    {
        const float grosor = 16;
        float lado = Math.Min(r.Width, r.Height) - grosor * 2;
        float x = r.Center.X - lado / 2;
        float y = r.Center.Y - lado / 2;

        canvas.StrokeSize = grosor;
        canvas.StrokeLineCap = LineCap.Round;

        canvas.StrokeColor = Color.FromArgb("#2AFFFFFF");
        canvas.DrawEllipse(x, y, lado, lado);

        if (Progress > 0.003f)
        {
            float p = Math.Min(Progress, 0.999f);
            canvas.StrokeColor = ArcColor;
            canvas.DrawArc(x, y, lado, lado, 90, 90 - 360 * p, true, false);
        }
    }
}

public partial class MainPage : ContentPage
{
    const double PesoSeguimiento = 0.3;
    const double PesoExamen = 0.2;

    readonly RingDrawable ring = new();
    bool animando;

    public MainPage()
    {
        InitializeComponent();

        pickerEstudiante.ItemsSource = new List<string>
        {
            "Estudiante 1", "Estudiante 2", "Estudiante 3",
            "Estudiante 4", "Estudiante 5"   // cambialos por los nombres de tus companeros
        };
        dpFecha.Date = DateTime.Today;
        ringView.Drawable = ring;
    }

    // ---------- Entrada escalonada + fondo animado ----------
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        animando = true;
        _ = AnimarFondoAsync();
        _ = PulsoBadgeAsync();

        View[] elementos = { header, cardEstudiante, cardP1, cardP2, cardFecha, btnCalcular };
        foreach (var v in elementos) { v.Opacity = 0; v.TranslationY = 60; }

        foreach (var v in elementos)
        {
            _ = v.FadeToAsync(1, 500, Easing.CubicOut);
            _ = v.TranslateToAsync(0, 0, 550, Easing.SpringOut);
            await Task.Delay(120);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        animando = false;
    }

    async Task AnimarFondoAsync()
    {
        while (animando)
        {
            await Task.WhenAll(
                orb1.TranslateToAsync(70, 90, 5500, Easing.SinInOut),
                orb2.TranslateToAsync(-80, -70, 5500, Easing.SinInOut),
                orb3.TranslateToAsync(60, -80, 5500, Easing.SinInOut));
            if (!animando) break;
            await Task.WhenAll(
                orb1.TranslateToAsync(0, 0, 5500, Easing.SinInOut),
                orb2.TranslateToAsync(0, 0, 5500, Easing.SinInOut),
                orb3.TranslateToAsync(0, 0, 5500, Easing.SinInOut));
        }
    }

    async Task PulsoBadgeAsync()
    {
        while (animando)
        {
            await badge.ScaleToAsync(1.08, 1400, Easing.SinInOut);
            await badge.ScaleToAsync(1.0, 1400, Easing.SinInOut);
        }
    }

    // ---------- Validacion numerica y de rango (0.1 - 10) ----------
    static bool TryNota(string? texto, out double valor)
    {
        valor = 0;
        if (string.IsNullOrWhiteSpace(texto)) return false;

        texto = texto.Trim().Replace(',', '.');   // acepta 8,5 y 8.5

        return double.TryParse(texto, NumberStyles.AllowDecimalPoint,
                               CultureInfo.InvariantCulture, out valor)
               && valor >= 0.1 && valor <= 10;
    }

    static double Parcial(double seguimiento, double examen)
        => seguimiento * PesoSeguimiento + examen * PesoExamen;

    // ---------- Vista previa en vivo ----------
    void OnNotaChanged(object? sender, TextChangedEventArgs e)
    {
        if (TryNota(entSeg1.Text, out var s1) && TryNota(entEx1.Text, out var e1))
        {
            double p = Parcial(s1, e1);
            lblP1.Text = p.ToString("F2");
            _ = barP1.ProgressTo(p / 5, 350, Easing.CubicOut);
        }
        else { lblP1.Text = "--"; _ = barP1.ProgressTo(0, 250, Easing.CubicOut); }

        if (TryNota(entSeg2.Text, out var s2) && TryNota(entEx2.Text, out var e2))
        {
            double p = Parcial(s2, e2);
            lblP2.Text = p.ToString("F2");
            _ = barP2.ProgressTo(p / 5, 350, Easing.CubicOut);
        }
        else { lblP2.Text = "--"; _ = barP2.ProgressTo(0, 250, Easing.CubicOut); }

        // Texto en rojo si el valor es invalido
        if (sender is Entry ent)
            ent.TextColor = string.IsNullOrEmpty(ent.Text) || TryNota(ent.Text, out _)
                ? Colors.White : Colors.Tomato;
    }

    // ---------- Avatar con la inicial ----------
    async void OnEstudianteChanged(object? sender, EventArgs e)
    {
        lblPlaceholder.IsVisible = pickerEstudiante.SelectedIndex < 0;

        if (pickerEstudiante.SelectedItem is string nombre && nombre.Length > 0)
        {
            lblInicial.Text = nombre.Substring(0, 1).ToUpper();
            await avatar.ScaleToAsync(0.7, 90);
            await avatar.ScaleToAsync(1, 280, Easing.SpringOut);
        }
    }

    // ---------- Efecto al enfocar campos ----------
    void OnFocused(object? sender, FocusEventArgs e)
    {
        if (sender is Entry ent && ent.Parent is Border b)
        {
            b.Stroke = Color.FromArgb("#A78BFA");
            b.BackgroundColor = Color.FromArgb("#26FFFFFF");
            _ = b.ScaleToAsync(1.02, 150, Easing.CubicOut);
        }
    }

    void OnUnfocused(object? sender, FocusEventArgs e)
    {
        if (sender is Entry ent && ent.Parent is Border b)
        {
            b.Stroke = Color.FromArgb("#22FFFFFF");
            b.BackgroundColor = Color.FromArgb("#14FFFFFF");
            _ = b.ScaleToAsync(1, 150, Easing.CubicOut);
        }
    }

    // ---------- Boton ----------
    async void OnBtnPressed(object? s, EventArgs e) => await btnCalcular.ScaleToAsync(0.94, 80);
    async void OnBtnReleased(object? s, EventArgs e) => await btnCalcular.ScaleToAsync(1, 160, Easing.SpringOut);

    // ---------- Sacudida para errores ----------
    static async Task Sacudir(View v)
    {
        foreach (var x in new double[] { -12, 12, -8, 8, -4, 4, 0 })
            await v.TranslateToAsync(x, 0, 45);
    }

    // ---------- Calcular ----------
    async void OnCalcularClicked(object? sender, EventArgs e)
    {
        if (pickerEstudiante.SelectedIndex < 0)
        {
            await Sacudir(cardEstudiante);
            await DisplayAlertAsync("Falta dato", "Selecciona un estudiante.", "OK");
            return;
        }

        var campos = new (Entry entry, string nombre, View tarjeta)[]
        {
            (entSeg1, "Nota Seguimiento 1", cardP1),
            (entEx1,  "Examen 1",           cardP1),
            (entSeg2, "Nota Seguimiento 2", cardP2),
            (entEx2,  "Examen 2",           cardP2),
        };

        var valores = new double[4];
        for (int i = 0; i < campos.Length; i++)
        {
            if (!TryNota(campos[i].entry.Text, out valores[i]))
            {
                await Sacudir(campos[i].tarjeta);
                await DisplayAlertAsync("Dato invalido",
                    $"{campos[i].nombre} debe ser un numero entre 0.1 y 10.", "OK");
                campos[i].entry.Focus();
                return;
            }
        }

        double p1 = Math.Round(Parcial(valores[0], valores[1]), 2);
        double p2 = Math.Round(Parcial(valores[2], valores[3]), 2);
        double final = Math.Round(p1 + p2, 2);

        string estado; Color color;
        if (final >= 7) { estado = "APROBADO"; color = Color.FromArgb("#22C55E"); }
        else if (final >= 5) { estado = "COMPLEMENTARIO"; color = Color.FromArgb("#F59E0B"); }
        else { estado = "REPROBADO"; color = Color.FromArgb("#EF4444"); }

        // Preparar tarjeta de resultado
        lblFinal.Text = "0.00";
        lblResP1.Text = p1.ToString("F2");
        lblResP2.Text = p2.ToString("F2");
        lblEstado.Text = estado;
        lblEstado.TextColor = color;
        chipEstado.Stroke = color;
        chipEstado.BackgroundColor = color.WithAlpha(0.18f);
        ring.ArcColor = color;
        ring.Progress = 0;
        ringView.Invalidate();

        cardResultado.IsVisible = true;
        cardResultado.Opacity = 0;
        cardResultado.Scale = 0.85;
        _ = cardResultado.FadeToAsync(1, 400);
        _ = cardResultado.ScaleToAsync(1, 500, Easing.SpringOut);

        await Task.Delay(120);
        _ = scroll.ScrollToAsync(cardResultado, ScrollToPosition.Start, true);

        // Contador + anillo animados
        var anim = new Animation(v =>
        {
            lblFinal.Text = v.ToString("F2");
            ring.Progress = (float)(v / 10);
            ringView.Invalidate();
        }, 0, final);
        anim.Commit(this, "ContadorFinal", 16, 1100, Easing.CubicOut);

        // Display Alert con saltos de linea
        string mensaje =
            $"Nombre: {pickerEstudiante.SelectedItem}\n" +
            $"Fecha: {dpFecha.Date:dd/MM/yyyy}\n" +
            $"Nota Parcial 1: {p1:F2}\n" +
            $"Nota Parcial 2: {p2:F2}\n" +
            $"Nota Final: {final:F2}\n" +
            $"Estado: {estado}";

        await DisplayAlertAsync("Resultado", mensaje, "Aceptar");
    }
}