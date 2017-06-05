// Copyright 2017 Steffen Kiess <kiess@ki4.de>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;

namespace DBus
{
	public class DisposableList : IDisposable
	{
		List<IDisposable> list = new List<IDisposable> ();

		public void Dispose ()
		{
			lock (list) {
				foreach (var obj in list)
					obj.Dispose ();
			}
		}

		public void Add (IDisposable obj)
		{
			if (obj == null)
				throw new ArgumentNullException ("obj");

			list.Add (obj);
		}
	}
}

// Local Variables:
// tab-width: 4
// c-basic-offset: 4
// indent-tabs-mode: t
// End:
