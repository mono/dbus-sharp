// Copyright 2011 Bertrand Lorentz <bertrand.lorentz@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NUnit.Framework;
using DBus;
using org.freedesktop.DBus;

namespace DBus.Tests
{
	[TestFixture]
	public class RenamedInterfaceTest
	{
		string bus_name = "org.dbussharp.restaurant";
		ObjectPath path = new ObjectPath ("/org/dbussharp/restaurant");

		[Test, Ignore]
		public void FirstInterface ()
		{
			var restaurant = new StandingRestaurant ();
			Assert.AreEqual (Bus.Session.RequestName (bus_name), RequestNameReply.PrimaryOwner);
			Bus.Session.Register (path, restaurant);

			try {
				Assert.AreEqual ("cheese", GetFood (false));
			} finally {
				Bus.Session.ReleaseName (bus_name);
				Bus.Session.Unregister (path);
			}
		}
		
		[Test, Ignore]
		public void SecondInterface ()
		{
			var restaurant = new SeatedRestaurant ();
			Bus.Session.Register (path, restaurant);
			Assert.AreEqual (Bus.Session.RequestName (bus_name), RequestNameReply.PrimaryOwner);

			try {
				Assert.AreEqual ("bacon", GetFood (true));
			} finally {
				Bus.Session.ReleaseName (bus_name);
				Bus.Session.Unregister (path);
			}
		}
		
		string GetFood (bool second)
		{
			IRestaurantBase restaurant = null;
			if (second)
				restaurant = Bus.Session.GetObject<IRestaurantv2> (bus_name, path);
			else
				restaurant = Bus.Session.GetObject<IRestaurant> (bus_name, path);
			return restaurant.Food ();
		}
	}
	
	interface IRestaurantBase { string Food (); }
	[Interface ("org.dbussharp.restaurant")] interface IRestaurant : IRestaurantBase { }
	[Interface ("org.dbussharp.restaurant.table")] interface IRestaurantv2 : IRestaurantBase { }

	public class StandingRestaurant : IRestaurant
	{
		public string Food ()
		{
			return "cheese";
		}
	}

	public class SeatedRestaurant : IRestaurantv2
	{
		public string Food ()
		{
			return "bacon";
		}
	}
}

