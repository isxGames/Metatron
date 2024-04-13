namespace Metatron.Core.Interfaces
{
	public interface ICorporateHangarArray : ICargoContainer
	{
		bool IsActiveContainerFull { get; }
		bool IsActiveContainerHalfFull { get; }
		bool IsInitialized { get; }
		bool IsCleanedUpOutOfFrame { get; }
		void InitializeCorporateHangars();
	}
}