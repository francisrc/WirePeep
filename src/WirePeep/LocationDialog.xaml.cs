﻿#region Using Directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Menees.Windows.Presentation;

#endregion

namespace WirePeep
{
	public partial class LocationDialog : ExtendedDialog
	{
		#region Constructors

		public LocationDialog()
		{
			this.InitializeComponent();
		}

		#endregion

		#region Public Methods

		public bool Execute(Window owner, IReadOnlyList<PeerGroup> peerGroups, ref Location location)
		{
			this.Owner = owner;

			this.peerGroups.ItemsSource = peerGroups.OrderBy(group => group.Name).ToList();

			if (location == null)
			{
				this.Title = "Add Location";
			}
			else
			{
				this.name.Text = location.Name;
				this.address.Text = location.Address.ToString();
				this.peerGroups.SelectedItem = location.PeerGroup;
			}

			bool result = false;
			if (this.ShowDialog() ?? false)
			{
				List<string> errors = new List<string>();
				Location newLocation = this.TryGetLocation(errors, location?.Id);
				if (newLocation != null
					&& errors.Count == 0
					&& (location == null
						|| location.Name != newLocation.Name
						|| !location.Address.Equals(newLocation.Address)
						|| location.PeerGroup.Id != newLocation.PeerGroup.Id))
				{
					location = newLocation;
					result = true;
				}
			}

			return result;
		}

		#endregion

		#region Private Methods

		private Location TryGetLocation(IList<string> errors, Guid? id = null)
		{
			string name = this.name.Text.Trim();
			if (string.IsNullOrEmpty(name))
			{
				errors.Add("The name must be non-empty and non-whitespace.");
			}

			if (!IPAddress.TryParse(this.address.Text.Trim(), out IPAddress address))
			{
				errors.Add("The address must be a valid IPv4 or IPv6 address.");
			}

			PeerGroup peerGroup = this.peerGroups.SelectedItem as PeerGroup;
			if (peerGroup == null)
			{
				errors.Add("A peer group must be selected.");
			}

			Location result = errors.Count == 0 ? new Location(peerGroup, name, address, id) : null;
			return result;
		}

		#endregion

		#region Private Event Handlers

		private void OKClicked(object sender, RoutedEventArgs e)
		{
			List<string> errors = new List<string>();

			Location location;
			using (new WaitCursor())
			{
				location = this.TryGetLocation(errors);
				if (location == null && errors.Count == 0)
				{
					errors.Add("Unable to create new location instance.");
				}
			}

			if (errors.Count > 0)
			{
				WindowsUtility.ShowError(this, string.Join(Environment.NewLine, errors));
			}
			else
			{
				bool pingOk = true;
				const int WaitMilliseconds = 200;
				using (Pinger pinger = new Pinger(TimeSpan.FromMilliseconds(WaitMilliseconds)))
				{
					if (!pinger.CanPing(location.Address))
					{
						// Occasionally, a ping will be lost, and it's ok. The user might also want to configure
						// a site to watch that is known to be intermittently available.
						pingOk = WindowsUtility.ShowQuestion(this, "The specified address did not respond to a ping. Continue?", null, false);
					}
				}

				if (pingOk)
				{
					this.DialogResult = true;
				}
			}
		}

		#endregion
	}
}