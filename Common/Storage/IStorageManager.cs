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
        ILucentRepository<T> GetRepository<T>() where T : new();
    }
}