using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Metatron.Core
{
	public interface ICriticallyBlocking
	{
		bool CriticallyBlock();
	}
}
