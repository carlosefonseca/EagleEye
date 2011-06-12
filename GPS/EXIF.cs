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
	public class Exif : EEPluginInterface {
		private Persistence persistence;
		private Dictionary<long, Boolean> PluginData;
		
		#region EEPluginInterface Members

		public void Init() {
			persistence = new Persistence(this.Id() + ".eep.db");
			if (persistence.existed) {
				Load();
			} else {
				PluginData = new Dictionary<long, Boolean>();
			}
		}

		public ImageCollection processImageCollection(ImageCollection ic) {
			foreach (Image i in ic.ToList()) {
				if (i.ContainsPluginData("device")) continue;
				String device = "";
				if (i.ContainsExif("Make")) {
					device = (String)i.Exif("Make")+" ";
				}
				if (i.ContainsExif("Model")) {
					device += (String)i.Exif("Model");
				}
				i.SetPluginData("device", device);
			}
			return null;
		}

		public String Id() {
			return "exif";
		}

		public override String ToString() {
			return "EXIF";
		}

		public String ImageInfo(Image i) {
			return i.ToString();
		}

		public String ImageToString(Image i) {
			return i.ToString();
		}

		public void Load() {
			PluginData = persistence.Read<long, Boolean>(Converters.ReadLong, Converters.ReadBoolean);
		}

		public void Save() {
			foreach (KeyValuePair<long, Boolean> kv in PluginData) {
				persistence.Put(kv.Key.ToString(), kv.Value.ToString());
			}
		}


		public String generateMetadata() {
			return "not generated";
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
