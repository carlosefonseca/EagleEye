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
		private Persistence datePersistence;
		private Dictionary<long, Coord> PluginData;
		private Dictionary<long, DateTime> ImageDates;
		private Dictionary<DateTime, List<long>> DatesOfImages;
		
		#region EEPluginInterface Members

		public void Init() {
			persistence = new Persistence(this.Id() + ".eep.db");
			if (persistence.existed) {
				Load();
			} else {
				PluginData = new Dictionary<long, Coord>();
			}
			
			datePersistence = new Persistence(this.Id() + "-dates.eep.db");
			if (datePersistence.existed) {
				LoadDates();
			} else {
				DatesOfImages = new Dictionary<DateTime, List<long>>();
			}
		}

		public ImageCollection processImageCollection(ImageCollection ic) {/*
			ImageCollection result = ic.ImagesWithExifKey("GPSPosition");
			foreach (Image i in ic.ToList()) {
				persistence.Put<Coord>(i.id.ToString(), new Coord((string)i.Exif("GPSLatitude"), (string)i.Exif("GPSLongitude")));
			}
			return result;*/
//			SortedImageCollection sic = ic.ImagesWithAnyExifKeys(new String[]{"CreateDate", "DateCreated"}).ToSortable();
/*			foreach (KeyValuePair<long,Image> kv in ic.TheDictionary()) {
				DateTime key = kv.Value.Date();
				if (DatesOfImages.ContainsKey(key)) {
				DatesOfImages.Add(, kv.Key);
			}
			DatesOfImages.*/
			return null;
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

		public void Load() {
			ConvertFromBytes<Coord> ReadCoord = delegate(byte[] bytes) {
				return new Coord(bytes);
			};
			PluginData = persistence.Read<long, Coord>(Converters.ReadLong, ReadCoord);
		}

		public void LoadDates() {
			ImageDates = datePersistence.Read<long, DateTime>(Converters.ReadLong, Converters.ReadDateTime);
		}

		public void Save() {
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
