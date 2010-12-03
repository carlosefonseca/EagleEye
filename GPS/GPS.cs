using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EagleEye.Common;
using EagleEye.Plugins.FeatureExtraction;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace EEPlugin {
	public class GPS : EEPluginInterface {
		private Persistence persistence;
		private Dictionary<long, Coord> PluginData = new Dictionary<long, Coord>();
		#region EEPluginInterface Members

		public void Init() { }

		public ImageCollection processImageCollection(ImageCollection ic) {
			return ic.ImagesWithExifKey("GPSPosition");
		}

		public String Id() {
			return "gps";
		}

		public override String ToString() {
			return "GPS";
		}

		public String ImageInfo(Image i) {
			return (string)i.Exif("GPSPosition");
		}

		public String ImageToString(Image i) {
			return i.ToStringWithExif("GPSPosition");
		}

		public void Load(string dir) {
		}

		public void Save(string dir) {
			if (persistence == null)
				persistence = new Persistence(this.Id() + ".eep.db");
			foreach (KeyValuePair<long, Coord> kv in PluginData) {
				persistence.Put<Coord>(kv.Key.ToString(), kv.Value);
			}
		}


		#endregion EEPluginInterface Members
	}




	[Serializable]
	public class Coord : EEPersistable<Coord> {
		public string lat, lng;

		public Coord(string lat, string lng) {
			this.lat = lat;
			this.lng = lng;
		}

		public override string ToString() {
			return lat + ", " + lng;
		}

		#region Serialization
		public Coord(byte[] bytes) {
			/* Fill in the fields from the buffer. */
			BinaryFormatter formatter = new BinaryFormatter();
			MemoryStream memStream = new MemoryStream(bytes);
			Coord tmp = (Coord)formatter.Deserialize(memStream);

			this.lat = tmp.lat;
			this.lng = tmp.lng;
			memStream.Close();
		}
		public Coord Set(byte[] bytes) { return new Coord(bytes); }

		public byte[] GetBytes() {
			BinaryFormatter formatter = new BinaryFormatter();
			MemoryStream memStream = new MemoryStream();
			formatter.Serialize(memStream, this);
			byte[] bytes = memStream.GetBuffer();
			memStream.Close();
			return bytes;
		}
		#endregion Serialization

	}

}
