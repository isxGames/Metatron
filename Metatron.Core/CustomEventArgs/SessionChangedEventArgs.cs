namespace Metatron.Core.CustomEventArgs
{
	public class SessionChangedEventArgs : System.EventArgs
	{
		public bool InSpace;

		public SessionChangedEventArgs(bool inSpace)
		{
			InSpace = inSpace;
		}
	}
}