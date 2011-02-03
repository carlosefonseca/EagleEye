using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing.Imaging;

namespace EagleEye.Common {
	public class Thumbnails {
		#region Singleton
		private static Thumbnails _instance;

		public static Thumbnails Get() {
			if (_instance == null) {
				_instance = new Thumbnails();
			}
			return _instance;
		}
		#endregion Singleton

		Persistence persistence;

		private Thumbnails() {
			persistence = new Persistence("Thumbnails");
		}

		public System.Drawing.Bitmap GetThumbnail(long imgId) {
			return persistence.Get<System.Drawing.Bitmap>(imgId, Converters.ReadBitmap);
		}

		public byte[] GenerateThumbnailData(Image i) {
			System.Drawing.Image thumb = i.GenerateThumbnail("");
			
			//Compression stuff
			ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
			System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
			EncoderParameters myEncoderParameters = new EncoderParameters(1);
			EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 50L);
			myEncoderParameters.Param[0] = myEncoderParameter;

			//Saving to byte[]
			MemoryStream memStream = new MemoryStream();
			thumb.Save(memStream, jpgEncoder, myEncoderParameters);
			byte[] bytes = memStream.GetBuffer();
			memStream.Close();
			return bytes;
		}

		private ImageCodecInfo GetEncoder(ImageFormat format) {
			ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
			foreach (ImageCodecInfo codec in codecs) {
				if (codec.FormatID == format.Guid) {
					return codec;
				}
			}
			return null;
		}

		public void PutThumbnailInDB(Image i, byte[] data) {
			
		}

		public void GenerateAndSaveThumbnail(Image i) {
			if (File.Exists(i.path)) {
				byte[] data = GenerateThumbnailData(i);
				if (data != null) {
					persistence.Put(i.id.ToString(), data);
				}
			}
		}

		public bool ThumbnailExists(Image i) {
			return persistence.ExistsKey(i.id.ToString());
		}
	}
}
