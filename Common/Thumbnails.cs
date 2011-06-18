using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;

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

		private const int ThumbSize = 200;
		private Persistence persistence;


		private Thumbnails() {
			persistence = new Persistence("Thumbnails");

			//Set Compression stuff
			jpgEncoder = GetEncoder(ImageFormat.Jpeg);
			System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
			myEncoderParameters = new EncoderParameters(1);
			EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 50L);
			myEncoderParameters.Param[0] = myEncoderParameter;
		}

		public System.Drawing.Bitmap GetThumbnail(Image i) {
			if (ThumbnailExists(i)) {
				return persistence.Get<System.Drawing.Bitmap>(i.id, Converters.ReadBitmap);
			} else {
				GenerateAndSaveThumbnail(i);
				return persistence.Get<System.Drawing.Bitmap>(i.id, Converters.ReadBitmap);
			}
		}


		// Cenas necessárias para o processo e que não mudam
		private ImageCodecInfo jpgEncoder;
		private EncoderParameters myEncoderParameters;
		System.Drawing.Image.GetThumbnailImageAbort abort = delegate {
			Console.WriteLine("THUMBNAIL ABORTED!");
			return false;
		};
		IntPtr intptr = IntPtr.Zero;
		MemoryStream memStream;
		System.Drawing.Image thumb;

		/// <summary>
		/// Generates a thumbnail on the filesystem. Sets the i.v. thumbnail
		/// </summary>
		/// <param name="path">The FOLDER where the thumbnail will be created</param>
		/// <returns>Full path</returns>
		public byte[] GenerateThumbnailData(Image i) {
			int smallside = ThumbSize, newWidth, newHeight;

			if (!File.Exists(i.path)) return null;
			Bitmap orig = new Bitmap(i.path);
			if (orig.Size.Height < orig.Size.Width) {
				newHeight = smallside;
				newWidth = orig.Size.Width * smallside / orig.Size.Height;
			} else {
				newWidth = smallside;
				newHeight = orig.Size.Height * smallside / orig.Size.Width;
			}
			thumb = orig.GetThumbnailImage(newWidth, newHeight, abort, intptr);
			if (thumb == null) { throw new Exception("The thumbnail is null :S "); }

			//Saving to byte[]
			memStream = new MemoryStream();
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
			byte[] data = GenerateThumbnailData(i);
			if (data != null) {
				persistence.Put(i.id.ToString(), data);
			}
		}

		public bool ThumbnailExists(Image i) {
			return persistence.ExistsKey(i.id.ToString());
		}

		public bool ThumbnailExists(long id) {
			return persistence.ExistsKey(id.ToString());
		}
	}
}
