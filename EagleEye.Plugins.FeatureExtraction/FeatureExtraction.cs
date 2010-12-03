using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EagleEye.Common;

namespace EagleEye.Plugins.FeatureExtraction {
	public interface EEPluginInterface {
		void Init();

		ImageCollection processImageCollection(ImageCollection ic);

		String Id();

		String ToString();

		String ImageInfo(Image i);

		String ImageToString(Image i);

		void Load(string dir);
		void Save(string dir);
	}
}
