namespace Common
{
        public interface IRefitServiceResolver
        {
            T GetRefitService<T>(string serviceUrl);
        }
}
