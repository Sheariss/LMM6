using System;

namespace Atlas.Presentation.SOS.BlueScreen
{
    public sealed class BlueScreenEngine
    {
        public const string DefaultMessage =
            "Your PC ran into a problem and needs to restart. We're\n" +
            "just collecting some error info, and then we'll restart for\n" +
            "you.";

        public const string DefaultStopCode = "CRITICAL_PROCESS_DIED";

        public bool IsVisible { get; private set; }
        public bool IsRunning { get; private set; }

        public string Message { get; private set; } = DefaultMessage;
        public string StopCode { get; private set; } = DefaultStopCode;
        public string QRPayload { get; private set; } = string.Empty;

        public int Progress { get; private set; }

        public bool IsComplete => Progress == 100;

        public bool IsRestartReady =>
            IsRunning &&
            elapsedSeconds >= durationSeconds + completionHoldSeconds;

        private double elapsedSeconds;
        private double durationSeconds;
        private double completionHoldSeconds;

        public void Begin(
            string message,
            string stopCode,
            string qrPayload,
            float duration,
            float completionHold)
        {
            ValidateTiming(duration, completionHold);

            Message = string.IsNullOrWhiteSpace(message)
                ? DefaultMessage
                : message;

            StopCode = string.IsNullOrWhiteSpace(stopCode)
                ? DefaultStopCode
                : stopCode.Trim();

            QRPayload = qrPayload ?? string.Empty;

            durationSeconds = duration;
            completionHoldSeconds = completionHold;

            elapsedSeconds = 0;
            Progress = 0;
            IsVisible = true;
            IsRunning = true;
        }

        // Returns true when the displayed percentage changes.
        public bool Advance(float deltaSeconds)
        {
            if (!IsRunning)
                return false;

            if (float.IsNaN(deltaSeconds) ||
                float.IsInfinity(deltaSeconds) ||
                deltaSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }

            elapsedSeconds = Math.Min(
                elapsedSeconds + deltaSeconds,
                durationSeconds + completionHoldSeconds);

            int nextProgress = (int)Math.Floor(
                Math.Min(elapsedSeconds / durationSeconds, 1d) * 100d);

            if (nextProgress == Progress)
                return false;

            Progress = nextProgress;
            return true;
        }

        public void Stop()
        {
            IsRunning = false;
            IsVisible = false;
        }

        public static void ValidateTiming(
            float duration,
            float completionHold)
        {
            if (float.IsNaN(duration) ||
                float.IsInfinity(duration) ||
                duration <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            if (float.IsNaN(completionHold) ||
                float.IsInfinity(completionHold) ||
                completionHold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(completionHold));
            }
        }
    }
}