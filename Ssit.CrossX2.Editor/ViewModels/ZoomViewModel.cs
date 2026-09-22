using System;
using CommunityToolkit.Mvvm.Input;
using Ssit.CrossX2.Editor.Models;

namespace Ssit.CrossX2.Editor.ViewModels
{
    public class ZoomViewModel: ViewModelBase
    {
        private class ZoomData : IZoomData
        {
            public decimal Zoom { get; set; } = 1;
            public void RequestSave()
            {
            }
        }
        
        private readonly Action _onZoom;
        private readonly IZoomData _zoomData;
        public IRelayCommand ZoomOutCommand { get; }
        public IRelayCommand ZoomInCommand { get; }

        public string Info
        {
            get;
            set => SetField(ref field, value);
        }

        public float Value => (float)_zoomData.Zoom;

        public ZoomViewModel(Action onZoom, IZoomData zoomData = null)
        {
            _onZoom = onZoom;
            _zoomData = zoomData ?? new ZoomData();

            if (_zoomData.Zoom == 0)
            {
                _zoomData.Zoom = 1;
            }

            ZoomInCommand = new RelayCommand(ZoomIn, () => _zoomData.Zoom < 8);
            ZoomOutCommand = new RelayCommand(ZoomOut, () => _zoomData.Zoom > 0.25m);
        
            UpdateZoom();
        }

        public void SetZoom(decimal zoom)
        {
            _zoomData.Zoom = zoom;
            _zoomData.RequestSave();
            
            ZoomInCommand.NotifyCanExecuteChanged();
            ZoomOutCommand.NotifyCanExecuteChanged();
            UpdateZoom();
            _onZoom?.Invoke();
        }

        private void UpdateZoom()
        {
            Info = $"{(int) (_zoomData.Zoom * 100)}%";
        }

        private void ZoomOut()
        {
            if (_zoomData.Zoom <= 1)
            {
                _zoomData.Zoom -= 0.25m;
            }
            else
            {
                _zoomData.Zoom -= 1;
            }
        
            if (_zoomData.Zoom < 0.25m)
            {
                _zoomData.Zoom = 0.25m;
            }
        
            ZoomInCommand.NotifyCanExecuteChanged();
            ZoomOutCommand.NotifyCanExecuteChanged();
            UpdateZoom();
            _onZoom?.Invoke();
            _zoomData.RequestSave();
        }

        private void ZoomIn()
        {
            if (_zoomData.Zoom < 1)
            {
                _zoomData.Zoom += 0.25m;
            }
            else
            {
                _zoomData.Zoom += 1;
            }

            if (_zoomData.Zoom > 8)
            {
                _zoomData.Zoom = 8;
            }
        
            ZoomInCommand.NotifyCanExecuteChanged();
            ZoomOutCommand.NotifyCanExecuteChanged();
            UpdateZoom();
            _onZoom?.Invoke();
            _zoomData.RequestSave();
        }
    }
}