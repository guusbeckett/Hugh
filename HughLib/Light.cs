using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace HughLib
{
    public class Light : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public enum Effect
        {
            EFFECT_COLORLOOP,
            EFFECT_NONE
        }

        public enum PredefinedColor
        {
            RED,
            GREEN,
            BLUE,
            YELLOW,
            ORANGE,
            PINK,
            WHITE
        }

        int _id;
        public int id
        {
            get
            {
                return this._id;
            }
            set
            {
                this._id = value;
                NotifyPropertyChanged();
            }
        }

        string _name;
        public string name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
                NotifyPropertyChanged();
            }
        }

        bool _on;
        public bool on
        {
            get
            {
                return this._on;
            }
            set
            {
                this._on = value;
                NotifyPropertyChanged();
            }
        }

        int _hue;
        public int hue
        {
            get
            {
                return this._hue;
            }
            set
            {
                this._hue = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("color");
            }
        }

        int _saturation;
        public int saturation
        {
            get
            {
                return this._saturation;
            }
            set
            {
                this._saturation = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("color");
            }
        }

        int _value;
        public int value
        {
            get
            {
                return this._value;
            }
            set
            {
                this._value = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("color");
            }
        }

        Effect _effect;
        public Effect effect
        {
            get
            {
                return this._effect;
            }
            set
            {
                this._effect = value;
                NotifyPropertyChanged();
            }
        }

        bool _reachable;
        public bool reachable
        {
            get
            {
                return this._reachable;
            }
            set
            {
                this._reachable = value;
                NotifyPropertyChanged();
            }
        }

        public SolidColorBrush color
        {
            get
            {
                if (this.reachable && this.on && this.effect == Effect.EFFECT_NONE)
                    return new SolidColorBrush(getColor());
                else
                    return new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            }
            private set
            {
                setColor(value.Color);
            }
        }
        public SolidColorBrush colorBackground
        {
            get
            {
                if (this.reachable)
                    return new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
                else
                    return new SolidColorBrush(Color.FromArgb(255, 170, 55, 55));
            }
            private set { }
        }
        public string colorText
        {
            get
            {
                if (this.reachable)
                {
                    if (this.on)
                    {
                        if (this.effect == Effect.EFFECT_COLORLOOP)
                            return "Loop";
                        else
                            return string.Empty;
                    }
                    else
                        return "Off";
                }
                else
                    return string.Empty;
            }
            private set { }
        }
        public bool colorloop
        {
            get
            {
                return this.effect == Effect.EFFECT_COLORLOOP;
            }
            set
            {
                if (value)
                    this.effect = Effect.EFFECT_COLORLOOP;
                else
                    this.effect = Effect.EFFECT_NONE;
            }
        }

        public Light(int id, string name, bool on, int hue, int saturation, int value, Effect effect, bool reachable)
        {
            this.id = id;
            this.name = name;
            this.on = on;
            this.hue = hue;
            this.saturation = saturation;
            this.value = value;
            this.effect = effect;
            this.reachable = reachable;
        }

        // NotifyPropertyChanged will raise the PropertyChanged event, 
        // passing the source property that is being updated.
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            //if (PropertyChanged != null)
            //{
            //   PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            //}
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Color getColor()
        {
            double hue = ((double)this.hue * 360.0f) / 65535.0f;
            double sat = (double)this.saturation / 255.0f;
            double val = (double)this.value / 255.0f;

            int r, g, b;
            HsvToRgb(hue, sat, val, out r, out g, out b);
            return Color.FromArgb(255, Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
        }

        public void setColor(Color color)
        {
            int r = Convert.ToInt32(color.R);
            int g = Convert.ToInt32(color.G);
            int b = Convert.ToInt32(color.B);

            double hue, sat, val;
            RGBtoHSV(r, g, b, out hue, out sat, out val);
        }

        private static void RGBtoHSV(double r, double g, double b, out double h, out double s, out double v)
        {
            double min, max, delta;

            min = Math.Min(r, Math.Min(g, b));
            max = Math.Max(r, Math.Max(g, b));

            v = max;				// v
            delta = max - min;

            if (max != 0)
                s = delta / max;		// s
            else
            {
                // r = g = b = 0		// s = 0, v is undefined
                s = 0;
                h = -1;
                return;
            }

            if (r == max)
                h = (g - b) / delta;		// between yellow & magenta
            else if (g == max)
                h = 2 + (b - r) / delta;	// between cyan & yellow
            else
                h = 4 + (r - g) / delta;	// between magenta & cyan

            h *= 60;				// degrees

            if (h < 0)
                h += 360;
        }

        private static void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            double H = h;
            while (H < 0)
            {
                H += 360;
            }
            while (H >= 360)
            {
                H -= 360;
            }

            double R, G, B;
            if (V <= 0)
                R = G = B = 0;
            else if (S <= 0)
                R = G = B = V;
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {
                    // Red is the dominant color
                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    // Green is the dominant color
                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;
                    // Blue is the dominant color
                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;
                    // Red is the dominant color
                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;
                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.
                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;
                    // The color is not defined, we should throw an error.
                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }

            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        private static int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}
