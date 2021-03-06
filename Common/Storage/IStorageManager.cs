namespace Lucent.Common.Storage
{
    /// <summary>
    /// Manager for storage repositories
    /// </summary>
    public interface IStorageManager
    {
        /// <summary>
        /// Gets a typed repository
        /// </summary>
        /// <typeparam name="T">The type of object to store</typeparam>
        /// <returns>A storage repository for that type</returns>
        IStorageRepository<T> GetRepository<T>() where T : IStorageEntity, new();

        /// <summary>
        /// Register a custom repository
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        void RegisterRepository<R, T>() where R : IStorageRepository<T> where T : IStorageEntity, new();
    }
}