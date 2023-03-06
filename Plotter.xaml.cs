using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WpfApp1
{
    class Szakasz
    {
        Line vonal;

        public Szakasz(Line vonal)
        {
            this.vonal = vonal;
        }

        public bool Lathato { get => vonal != null && this.vonal.Visibility == Visibility.Visible; }
        public String SzakaszInfo
        {
            get => $"({Math.Round(Vonal.X1, 1)} ; {Math.Round(Vonal.Y1, 1)})" +
                $"->({Math.Round(Vonal.X2, 1)} ; {Math.Round(Vonal.Y2, 1)})";
        }
        public Brush EcsetSzine { get => vonal.Stroke; }
        public int EcsetVastagsaga { get => (int)vonal.StrokeThickness; }
        public Line Vonal { get => vonal; set => vonal = value; }
    }

    enum Hibakezeles { KiveteltGeneral, FeluletenJelzi, NemanElnyomja };
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Plotter : Window
    {
        Hibakezeles hibakezelesModja = Hibakezeles.FeluletenJelzi;
        Random vel = new Random();
        Color keretAlap = Colors.LightGreen;
        int _keretSzelesseg;
        const int FejSzelesseg = 50;
        int _mozgasi_Sebesseg = 6; //millisecundum
        int _forgasi_Sebesseg = 10; //millisecundum
        const int tavolsagEgyeg = 1; // 1 DIU
        const int XpozicioKorrekcio = -FejSzelesseg / 2;
        const int YpozicioKorrekcio = -FejSzelesseg / 2;
        //Fej jellemzői
        Size papirMeret;
        Point fejPozicio;
        double fejIrany = 0;
        bool fejRajzlapon = false;
        ObservableCollection<Szakasz> szakaszok = new ObservableCollection<Szakasz>();

        //Használat jellemzői
        double rajzolvaMegtettUt = 0;
        double uressenMegtettUt = 0;

        //Eszközök
        int eszkozokSzama;
        int eszkozSzelessege = 50;
        //Minde belső (private) metődus esetén 0-tól kezdődő számozással dolgozunk, minden külső (public) esetben +1-től indulóval
        int aktivEszkoz = 0;
        List<Border> eszkozok = new List<Border>();
        List<Storyboard> varakozoAnimaciok = new List<Storyboard>();


        public double RajzolvaMegtettUt { get => rajzolvaMegtettUt; }
        public double UressenMegtettUt { get => uressenMegtettUt; }
        internal ObservableCollection<Szakasz> Szakaszok { get => szakaszok; set => szakaszok = value; }
        internal Hibakezeles HibakezelesModja { get => hibakezelesModja; set => hibakezelesModja = value; }

        /// <summary>
        /// A rajzlap méretének megadásával hozza létre az ablakot
        /// </summary>
        /// <param name="szelesseg">Rajzlap szélessége</param>
        /// <param name="magassag">Rajzlap magassága</param>
        public Plotter(int szelesseg, int magassag, int eszkozSzam)
        {
            InitializeComponent();
            this.eszkozokSzama = eszkozSzam;
            _keretSzelesseg = (int)Math.Round(brdKulsoKeret.BorderThickness.Left + brdKulsoKeret.Padding.Left);
            papirMeret.Width = szelesseg;
            papirMeret.Height = magassag;

            EszkozokElhelyezese();
        }
        /// <summary>
        /// Az aktuális képernyőfelbontás mellett a maximális méretet biztosítja
        /// </summary>
        public Plotter()
        {
            InitializeComponent();
            _keretSzelesseg = (int)Math.Round(brdKulsoKeret.BorderThickness.Left + brdKulsoKeret.Padding.Left);
            eszkozokSzama = 4;
            papirMeret.Width = (int)SystemParameters.PrimaryScreenWidth - eszkozSzelessege - _keretSzelesseg * 2;
            //Tálca kihagyása miatt van a 80
            papirMeret.Height = (int)SystemParameters.PrimaryScreenHeight - _keretSzelesseg * 2 - 80;
            EszkozokElhelyezese();
        }

        private void EszkozokElhelyezese()
        {
            rajzlap.Width = papirMeret.Width;
            rajzlap.Height = papirMeret.Height;
            colDefKeret.Width = new GridLength(papirMeret.Width + _keretSzelesseg * 2);
            this.Width = papirMeret.Width + _keretSzelesseg * 2 + eszkozSzelessege;
            this.Height = papirMeret.Height + _keretSzelesseg * 2;

            fejPozicio = new Point(0, 0);
            Canvas.SetLeft(Fej, fejPozicio.X + XpozicioKorrekcio);
            Canvas.SetTop(Fej, fejPozicio.Y + YpozicioKorrekcio);

            for (int eszkozIndex = 1; eszkozIndex <= eszkozokSzama; eszkozIndex++)
            {
                RowDefinition tempRowDef = new RowDefinition();
                tempRowDef.Height = new GridLength(50);
                grdBolcsok.RowDefinitions.Add(tempRowDef);
                Border ujEszkoz = new Border();
                ujEszkoz.Background = new SolidColorBrush(Colors.Transparent);
                ujEszkoz.Margin = new Thickness(6, 0, 0, 5);
                ujEszkoz.CornerRadius = new CornerRadius(0, 20, 20, 0);
                ujEszkoz.BorderBrush = new SolidColorBrush(keretAlap);
                ujEszkoz.BorderThickness = new Thickness(0, 5, 5, 5);
                Line minta = new Line();
                minta.Stroke = new SolidColorBrush(Colors.Transparent);
                minta.StrokeThickness = 0;
                minta.X1 = 1;
                minta.Y1 = 19;
                minta.X2 = 20;
                minta.Y2 = 19;
                ujEszkoz.Child = minta;
                Grid.SetRow(ujEszkoz, eszkozIndex);
                grdBolcsok.Children.Add(ujEszkoz);
                //todo Az eszköz fei a fejet
                //Grid.SetZIndex(ujEszkoz, -eszkozIndex);
                eszkozok.Add(ujEszkoz);
            }
            BehuzDokkolo(aktivEszkoz);
        }
        private void InditoKep(object sender, RoutedEventArgs e)
        {
            DoubleAnimation doubleAnimation = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(1200)));
            this.BeginAnimation(OpacityProperty, doubleAnimation);

        }
        private void HibaJelzes()
        {
            SolidColorBrush myBrush = new SolidColorBrush();
            myBrush.Color = Colors.Blue;

            ColorAnimation myColorAnimation = new ColorAnimation();
            myColorAnimation.From = keretAlap;
            myColorAnimation.To = Colors.Red;
            myColorAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(300));
            myColorAnimation.AutoReverse = true;
            myColorAnimation.RepeatBehavior = new RepeatBehavior(3);

            myBrush.BeginAnimation(SolidColorBrush.ColorProperty, myColorAnimation);
            brdKulsoKeret.BorderBrush = myBrush;
        }
        private Point ErkezesiPont_IranybolUthosszbol(double fejIrany, double utHossz)
        {
            if (fejIrany == 0 || fejIrany == 360)
            {
                return new Point(fejPozicio.X, fejPozicio.Y - utHossz);
            }
            else if (fejIrany == 90)
            {
                return new Point(fejPozicio.X + utHossz, fejPozicio.Y);
            }
            else if (fejIrany == 180)
            {
                return new Point(fejPozicio.X, fejPozicio.Y + utHossz);
            }
            else if (fejIrany == 270)
            {
                return new Point(fejPozicio.X - utHossz, fejPozicio.Y);
            }
            else if (fejIrany > 0 && fejIrany < 90)
            {
                return new Point(fejPozicio.X + utHossz * Math.Sin(fejIrany / 180 * Math.PI),
                                 fejPozicio.Y - utHossz * Math.Cos(fejIrany / 180 * Math.PI));
            }
            else if (fejIrany > 90 && fejIrany < 180)
            {
                return new Point(fejPozicio.X + utHossz * Math.Cos((fejIrany - 90) / 180 * Math.PI),
                                 fejPozicio.Y + utHossz * Math.Sin((fejIrany - 90) / 180 * Math.PI));
            }
            else if (fejIrany > 180 && fejIrany < 270)
            {
                return new Point(fejPozicio.X - utHossz * Math.Sin((fejIrany - 180) / 180 * Math.PI),
                                 fejPozicio.Y + utHossz * Math.Cos((fejIrany - 180) / 180 * Math.PI));
            }
            else
            {
                return new Point(fejPozicio.X - utHossz * Math.Cos((fejIrany - 270) / 180 * Math.PI),
                                 fejPozicio.Y - utHossz * Math.Sin((fejIrany - 270) / 180 * Math.PI));
            }

        }
        public double Irany_ErkezesiPontbol(Point indulasiPont, Point erkezesiPont)
        {
            double deltaX = Math.Abs(erkezesiPont.X - indulasiPont.X);
            double deltaY = Math.Abs(erkezesiPont.Y - indulasiPont.Y);
            double ujIrany;
            if (erkezesiPont.X == indulasiPont.X)
            {
                ujIrany = erkezesiPont.Y < indulasiPont.Y ? 0 : 180;
            }
            else if (erkezesiPont.Y == indulasiPont.Y)
            {
                ujIrany = erkezesiPont.X < indulasiPont.X ? 270 : 90;
            }
            else if (erkezesiPont.X > indulasiPont.X && erkezesiPont.Y < indulasiPont.Y)
            {
                ujIrany = Math.Atan(deltaX / deltaY) * 180 / Math.PI;
            }
            else if (erkezesiPont.X > indulasiPont.X && erkezesiPont.Y > indulasiPont.Y)
            {
                ujIrany = 90 + Math.Atan(deltaY / deltaX) * 180 / Math.PI;
            }
            else if (erkezesiPont.Y > indulasiPont.Y)
            {
                ujIrany = 180 + Math.Atan(deltaX / deltaY) * 180 / Math.PI;
            }
            else
            {
                ujIrany = 270 + Math.Atan(deltaY / deltaX) * 180 / Math.PI;
            }
            return ujIrany;
        }

        /*
        public bool MozgatasLehetseges(double utHossz)
        {
            return MozgatasLehetseges(ErkezesiPont_IranybolUthosszbol(fejIrany, utHossz));
        }
        */
        public void FejMozgatas(double utHossz)
        {
            Point ujPozicio = ErkezesiPont_IranybolUthosszbol(fejIrany, utHossz);
            if (VanE_HIBA_SzakaszAnimacio(ujPozicio,"Fejmozgatás"))
                return;
            UtemezAnimaciot(SzakaszAnimacio(ujPozicio));
        }

        private void UtemezAnimaciot(Storyboard animacio)
        {
            animacio.Begin();
            /*
            animacio.Completed += VarakozoAnimaciotIndit;
            if (varakozoAnimaciok.Count > 0)
            {
                varakozoAnimaciok.Add(animacio);
            }
            else
            {
                animacio.Begin();
            }*/
        }

        private void Animacio_Completed(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private bool VanE_HIBA_SzakaszAnimacio(Point ujPozicio, String metodus)
        {
            String hibaUzenet = "";
            if (ujPozicio.X == fejPozicio.X && ujPozicio.Y == fejPozicio.Y)
            {
                hibaUzenet = $"[{metodus}] A fej ott van, ahová küldi!";
            }
            else if (ujPozicio.X < 0 || ujPozicio.X > rajzlap.Width || ujPozicio.Y < 0 || ujPozicio.Y > rajzlap.Height)
            {
                hibaUzenet = $"[{metodus}] A célhely a rajzlapon kivül van! "+
                    $"A ({Math.Round(fejPozicio.X, 4)} ; {Math.Round(fejPozicio.Y, 4)})-ból" +
                    $" nem lehet a ({Math.Round(ujPozicio.X, 4)} ; {Math.Round(ujPozicio.Y, 4)}) pontba mozgatni!";
            }
            else
            {
                return false;
            }
            switch (hibakezelesModja)
            {
                case Hibakezeles.KiveteltGeneral:
                    throw new Exception(hibaUzenet);
                case Hibakezeles.FeluletenJelzi:
                    HibaJelzes();
                    MessageBox.Show(hibaUzenet);
                    break;
            }
            return true;
        }
        private Storyboard SzakaszAnimacio(Point ujPozicio)
        {


            double deltaX = Math.Abs(ujPozicio.X - fejPozicio.X);
            double deltaY = Math.Abs(ujPozicio.Y - fejPozicio.Y);

            double utHossza = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
            int uthozSzuksegesIdo = (int)Math.Round(utHossza / tavolsagEgyeg * _mozgasi_Sebesseg);

            Storyboard sbFejMozgatas = new Storyboard();
            DoubleAnimation doubleAnimation;
            if (fejRajzlapon)
            {
                Line animaltSzakasz = new Line();
                if (eszkozok[aktivEszkoz].Child is Line mintaSzakasz)
                {
                    animaltSzakasz.StrokeThickness = mintaSzakasz.StrokeThickness;
                }
                animaltSzakasz.Stroke = eszkozok[aktivEszkoz].Background;
                animaltSzakasz.X1 = fejPozicio.X;
                animaltSzakasz.Y1 = fejPozicio.Y;
                animaltSzakasz.X2 = fejPozicio.X;
                animaltSzakasz.Y2 = fejPozicio.Y;
                rajzlap.Children.Add(animaltSzakasz);
                Line taroltSzakasz = new Line();
                taroltSzakasz.X1 = fejPozicio.X;
                taroltSzakasz.Y1 = fejPozicio.Y;
                taroltSzakasz.X2 = ujPozicio.X;
                taroltSzakasz.Y2 = ujPozicio.Y;

                Szakaszok.Add(new Szakasz(animaltSzakasz));

                doubleAnimation = new DoubleAnimation(fejPozicio.X, ujPozicio.X,
                          new Duration(TimeSpan.FromMilliseconds(uthozSzuksegesIdo)));
                Storyboard.SetTarget(doubleAnimation, animaltSzakasz);
                Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(X2)"));
                sbFejMozgatas.Children.Add(doubleAnimation);

                doubleAnimation = new DoubleAnimation(fejPozicio.Y, ujPozicio.Y,
                                          new Duration(TimeSpan.FromMilliseconds(uthozSzuksegesIdo)));
                Storyboard.SetTarget(doubleAnimation, animaltSzakasz);
                Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(Y2)"));
                sbFejMozgatas.Children.Add(doubleAnimation);
                rajzolvaMegtettUt += utHossza;
            }
            else
            {
                uthozSzuksegesIdo /= 2;
                uressenMegtettUt += utHossza;
            }

            doubleAnimation = new DoubleAnimation(fejPozicio.X + XpozicioKorrekcio,
                                                  ujPozicio.X + XpozicioKorrekcio,
                                                  new Duration(TimeSpan.FromMilliseconds(uthozSzuksegesIdo)));
            Storyboard.SetTarget(doubleAnimation, Fej);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(Canvas.Left)"));
            sbFejMozgatas.Children.Add(doubleAnimation);

            doubleAnimation = new DoubleAnimation(fejPozicio.Y + XpozicioKorrekcio,
                                                  ujPozicio.Y + YpozicioKorrekcio,
                                                  new Duration(TimeSpan.FromMilliseconds(uthozSzuksegesIdo)));
            Storyboard.SetTarget(doubleAnimation, Fej);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(Canvas.Top)"));
            sbFejMozgatas.Children.Add(doubleAnimation);

            fejPozicio = ujPozicio;
            return sbFejMozgatas;

        }

        private Storyboard IranyValtasAbs(double ujIranySzog)
        {
            //todo Értelmezési tartomány [0,360[ lehet!!!
            double tempZaroSzog = ujIranySzog;

            if (ujIranySzog > fejIrany)
            {
                if (ujIranySzog - fejIrany > 180)
                {
                    tempZaroSzog -= 360;
                }
            }
            else
            {
                if (fejIrany - ujIranySzog > 180)
                {
                    tempZaroSzog += 360;
                }
            }

            Storyboard dbForgatokonyv = new Storyboard();
            RotateTransform forgatoTranszformacio = new RotateTransform();
            forgatoTranszformacio.CenterX = FejSzelesseg / 2;
            forgatoTranszformacio.CenterY = forgatoTranszformacio.CenterX;
            //forgatoTranszformacio.Angle = ujIrany;

            Fej.RenderTransform = forgatoTranszformacio;
            int forgasiIdo = (int)(_forgasi_Sebesseg * Math.Abs(tempZaroSzog - fejIrany));

            DoubleAnimation forgatoAnimacio = new DoubleAnimation(fejIrany, tempZaroSzog,
                                                  new Duration(TimeSpan.FromMilliseconds(forgasiIdo)));
            Storyboard.SetTarget(forgatoAnimacio, Fej);
            Storyboard.SetTargetProperty(forgatoAnimacio, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));

            dbForgatokonyv.Children.Add(forgatoAnimacio);
            fejIrany = ujIranySzog;
            return dbForgatokonyv;
        }


        /// <summary>
        /// A fejet a megadott szöggel fordítja balra vagy jobbra
        /// </summary>
        /// <param name="relSzog">Értelmezési tartomány: 0..360</param>
        public void FejForditas(double relSzog)
        {
            double ujIrany = fejIrany + relSzog;
            if (ujIrany > 360)
            {
                ujIrany %= 360;
            }
            while (ujIrany < 0)
            {
                ujIrany += 360;
            }
            UtemezAnimaciot(IranyValtasAbs(ujIrany));
        }

        public void FejLe()
        {
            this.fejRajzlapon = true;
            //todo vmi animáció!
        }

        public void FejFel()
        {
            this.fejRajzlapon = false;
            //todo vmi animáció!
        }
        public void SzakaszKi(int index)
        {
            if (this.szakaszok[index].Vonal.Visibility == Visibility.Hidden)
            {
                this.szakaszok[index].Vonal.Visibility = Visibility.Visible;
            }
            else
            {
                this.szakaszok[index].Vonal.Visibility = Visibility.Hidden;
            }
        }


        private void BehuzDokkolo(int bolcsoIndex)
        {
            Storyboard sbDokkoloMozgatas = new Storyboard();
            ThicknessAnimation taMargoAnimacio = new ThicknessAnimation();
            taMargoAnimacio.From = new Thickness(10, 0, 0, 5);
            taMargoAnimacio.To = new Thickness(-40, 0, 50, 5);
            taMargoAnimacio.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            Storyboard.SetTarget(taMargoAnimacio, eszkozok[bolcsoIndex]);
            Storyboard.SetTargetProperty(taMargoAnimacio, new PropertyPath(MarginProperty));
            sbDokkoloMozgatas.Children.Add(taMargoAnimacio);
            UtemezAnimaciot(sbDokkoloMozgatas);
        }

        private void KitolDokkolo(int bolcsoIndex)
        {
            Storyboard sbDokkoloMozgatas = new Storyboard();
            ThicknessAnimation taMargoAnimacio = new ThicknessAnimation();
            taMargoAnimacio.From = new Thickness(-40, 0, 50, 5);
            taMargoAnimacio.To = new Thickness(10, 0, 0, 5);
            taMargoAnimacio.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            Storyboard.SetTarget(taMargoAnimacio, eszkozok[bolcsoIndex]);
            Storyboard.SetTargetProperty(taMargoAnimacio, new PropertyPath(MarginProperty));
            sbDokkoloMozgatas.Children.Add(taMargoAnimacio);
            UtemezAnimaciot(sbDokkoloMozgatas);
        }

        private bool VanE_Hiba_EszkozCsere(int eszkozIndex, string metodus)
        {
            String hibaUzenet = "";
            if (eszkozIndex == aktivEszkoz)
            {
                hibaUzenet = $"[{metodus}] A megadott eszköz van most használatban!";
            }
            else if (fejRajzlapon)
            {
                hibaUzenet = $"[{metodus}] Előbb emelje fel a fejet!";
            }
            else if (eszkozok[eszkozIndex].Background is SolidColorBrush hatterszin && hatterszin.Color == Colors.Transparent)
            {
                hibaUzenet = $"[{metodus}] Üres eszközre nem cserélhet!";
            }
            else
            {
                return false;
            }
            switch (hibakezelesModja)
            {
                case Hibakezeles.KiveteltGeneral:
                    throw new Exception(hibaUzenet);
                case Hibakezeles.FeluletenJelzi:
                    HibaJelzes();
                    MessageBox.Show(hibaUzenet);
                    break;
            }
            return true;
        }
        public void EszkozCsere(int eszkozIndex)
        {
            eszkozIndex--;
            if (VanE_Hiba_EszkozCsere(eszkozIndex, "EszkozCsere"))
                return;

            Point tempPozicio = fejPozicio;
            KitolDokkolo(aktivEszkoz);
            Point eszkozHely = new Point(rajzlap.Width, 100 + eszkozIndex * eszkozSzelessege + eszkozSzelessege / 2);
            Storyboard animacio = SzakaszAnimacio(eszkozHely);
            aktivEszkoz = eszkozIndex;
            BehuzDokkolo(aktivEszkoz);
            animacio.Completed += (sender, e) =>
            {
                FejMozgatasForgatasNelkul(tempPozicio);
            };
            UtemezAnimaciot(animacio);
        }

        private void FejMozgatasForgatasNelkul(Point ujPozicio)
        {
            SzakaszAnimacio(ujPozicio).Begin();
        }

        public void FejMozgatas(Point ujPozicio)
        {
            if (VanE_HIBA_SzakaszAnimacio(ujPozicio,"FejMozgatas"))
                return;
            Storyboard animacio = IranyValtasAbs(Irany_ErkezesiPontbol(fejPozicio, ujPozicio));
            animacio.Completed += (sender, e) =>
            {
                UtemezAnimaciot(SzakaszAnimacio(ujPozicio));
            };
            UtemezAnimaciot(animacio);

        }

        public void EszkozBetoltese(int eszkozIndex, Color szin, int vastagsag)
        {
            eszkozIndex--;
            eszkozok[eszkozIndex].Background = new SolidColorBrush(szin);
            if (eszkozok[eszkozIndex].Child is Line mintaSzakasz)
            {
                //todo Az AreClose nem megfelelő számomra!
                mintaSzakasz.Stroke = new SolidColorBrush(Color.AreClose(Colors.Black, szin) ? Colors.White : Colors.Black);
                mintaSzakasz.StrokeThickness = vastagsag;
            }
        }

        private void VarakozoAnimaciotIndit(object? sender, EventArgs e)
        {
            if (varakozoAnimaciok.Count > 0)
            {
                Storyboard animacio = varakozoAnimaciok[0];
                animacio.Completed += VarakozoAnimaciotIndit;
                varakozoAnimaciok.RemoveAt(0);
                animacio.Begin();
            }
        }

        private void btnPlotterKikapcsolas(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnPlotterInformacio(object sender, RoutedEventArgs e)
        {

        }
    }

}
