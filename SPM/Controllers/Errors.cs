namespace SPM.Controllers
{
    public class Errors
    {
        public double ErrorRate { get; }

        public Errors(double errorRate)
        {
            ErrorRate = errorRate;
        }

        public bool ProduceError()
        {
            return Random.Shared.NextDouble() > ErrorRate;
        }
    }
}
