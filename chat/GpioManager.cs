using System.Device.Gpio;

namespace IoT {

    public class GpioManager : IDisposable {

        private GpioController _controller;
        public event EventHandler<GpioEventArgs> OnPhysicalStateChange = delegate { };
        
        private bool _isLedOn;
        private readonly int _pinInput;
        private readonly int _pinOutput;

        public GpioManager(int pinOutput, int pinInput) {
            _pinOutput = pinOutput;
            _pinInput = pinInput;
            _controller = new GpioController();

            _controller.OpenPin(_pinOutput, PinMode.Output);
            _controller.OpenPin(_pinInput, PinMode.Input);

            _controller.RegisterCallbackForPinValueChangedEvent(_pinInput, PinEventTypes.Falling,  (sender, args) => PhysicalStateChangeEvent(args));

        }

        private void TurnOnLight() {
            Console.WriteLine("Turning on the light...");

            _isLedOn = true;
            _controller.Write(_pinOutput, PinValue.High);
        }

        private void TurnOffLight() {
            Console.WriteLine("Turning off the light...");

            _isLedOn = false;
            _controller.Write(_pinOutput, PinValue.Low);
        } 

        public bool ToggleLight() {
            if(_isLedOn) TurnOffLight();
            else TurnOnLight();

            return _isLedOn;
        }

        public void Dispose() {
            if (_controller != null) {
                _controller.ClosePin(_pinOutput);
                _controller.ClosePin(_pinInput);
                _controller.Dispose();
            }
        }

        public void HandleVirtualStateChange(P2PEventArgs args) {
            ToggleLight();
        }

        public void PhysicalStateChangeEvent(PinValueChangedEventArgs args) {
            //TODO - Better handling of the IoT ids
            int id = args.PinNumber;

            OnPhysicalStateChange?.Invoke(this, new GpioEventArgs(id, ToggleLight() ? 1 : 0));
        }
    }
}

public class GpioEventArgs : EventArgs {
    public int State { get; }
    public int Id { get; }

    public GpioEventArgs(int id, int state) {
        State = state;
        Id = id;
    }
}
