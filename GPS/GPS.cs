using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EagleEye.Common;
using EagleEye.Plugins.FeatureExtraction;

namespace EEPlugin {
	public class GPS : EEPluginInterface {
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

		#endregion EEPluginInterface Members
	}
}
