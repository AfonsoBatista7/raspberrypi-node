using System.Device.Gpio;

namespace IoT {

    public class GpioManager : IDisposable {

        private GpioController _controller;
        public event EventHandler<IotEventData> OnPhysicalStateChange = delegate { };
        
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

            _controller.Write(_pinOutput, PinValue.High);
        }

        private void TurnOffLight() {
            Console.WriteLine("Turning off the light...");

            _controller.Write(_pinOutput, PinValue.Low);
        } 

        private void TurnOnOff() {
            if(_isLedOn) TurnOnLight();
            else TurnOffLight();
        }

        public void ToggleLight() {
            _isLedOn = !_isLedOn;

            TurnOnOff();
        }

        public void SetState(bool boolState) {
            if(boolState!=_isLedOn) {
                _isLedOn = boolState;
                
                TurnOnOff();
            }
        }

        public void Dispose() {

            TurnOffLight();

            if (_controller != null) {
                _controller.ClosePin(_pinOutput);
                _controller.ClosePin(_pinInput);
                _controller.Dispose();
            }
        }

        public void HandleVirtualStateChange(IotEventData args) {
            //TODO - handle objectId
            string objectId = args.Id;
            SetState(args.StateToBool());
        }

        public void PhysicalStateChangeEvent(PinValueChangedEventArgs args) {
            //TODO - Better handling of the IoT ids
            string id = args.PinNumber.ToString();

            ToggleLight();

            OnPhysicalStateChange?.Invoke(this, new IotEventData(id, _isLedOn));
        }
    }
}
