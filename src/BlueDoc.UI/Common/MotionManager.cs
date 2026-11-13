using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BlueDoc.UI.Common
{
    public class MotionManager : INotifyPropertyChanged
    {
        private static MotionManager _instance;
        public static MotionManager Instance => _instance ?? (_instance = new MotionManager());

        private bool _forceReducedMotion;
        private bool _isSoftwareRendering;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MotionManager()
        {
            try
            {
                int tier = RenderCapability.Tier >> 16;
                _isSoftwareRendering = (tier == 0);
            }
            catch
            {
                _isSoftwareRendering = false;
            }
        }

        public bool IsSoftwareRendering
        {
            get => _isSoftwareRendering;
            set
            {
                if (_isSoftwareRendering != value)
                {
                    _isSoftwareRendering = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAnimationAllowed));
                }
            }
        }

        public bool ForceReducedMotion
        {
            get => _forceReducedMotion;
            set
            {
                if (_forceReducedMotion != value)
                {
                    _forceReducedMotion = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAnimationAllowed));
                    OnPropertyChanged(nameof(IsReducedMotionPreferred));
                }
            }
        }

        public bool IsReducedMotionPreferred
        {
            get
            {
                if (ForceReducedMotion) return true;
                try
                {
                    // Check Windows OS Accessibility Setting: "Show animations in Windows"
                    return !SystemParameters.ClientAreaAnimation;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool IsAnimationAllowed => !IsReducedMotionPreferred && !IsSoftwareRendering;

        public TimeSpan PageTransitionDuration => TimeSpan.FromMilliseconds(IsAnimationAllowed ? 180 : 0);
        public TimeSpan PanelTransitionDuration => TimeSpan.FromMilliseconds(IsAnimationAllowed ? 160 : 0);
        public TimeSpan MicroInteractionDuration => TimeSpan.FromMilliseconds(IsAnimationAllowed ? 120 : 0);

        public void ApplyFadeIn(UIElement element, double from = 0.0, double to = 1.0, Action onCompleted = null)
        {
            if (element == null) return;

            if (!IsAnimationAllowed)
            {
                element.Opacity = to;
                onCompleted?.Invoke();
                return;
            }

            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = PageTransitionDuration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            if (onCompleted != null)
            {
                animation.Completed += (s, e) => onCompleted();
            }

            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        public void StopAnimations(UIElement element)
        {
            if (element == null) return;
            try
            {
                element.BeginAnimation(UIElement.OpacityProperty, null);
            }
            catch { }
        }
    }
}
