﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;
using System.Drawing.Imaging;

namespace EagleEye.Common {
	[Serializable]
	public class Image : EEPersistable<Image> {
		public long id = -1;
		public string path;
		public bool exifImported = false;
		public Dictionary<string, object> exif;
		private string thumbnail;

		public Image(string path) {
			this.path = Path.GetFullPath(path);
			exif = new Dictionary<string, object>();
		}

		public override string ToString() {
			return "IMG ID " + this.id + " @ " + this.path + " - " + exif.Count + " EXIF items";
		}

		public void Exif(string k, string v) {
			try {
				exif.Add(k, v);
			} catch (ArgumentException) {
				exif[k] = v;
			}
		}

		public object Exif(string k) {
			try {
				return this.exif[k];
			} catch (KeyNotFoundException) {
				return "";
			}
		}

		public int Exif(Dictionary<string, object> exif) {
			this.exif = exif;
			return this.exif.Count;
		}

		public Dictionary<string, object> Exif() {
			return exif;
		}

		public string Details() {
			string txt = "";
			foreach (string k in exif.Keys) {
				txt += k.PadRight(30, ' ') + "   >   " + exif[k] + "\n";
			}
			return txt;
		}

		public string Date() {
			if (exif.ContainsKey("DateCreated")) {
				Console.WriteLine(">> " + path + " > DateCreated");
				return exif["DateCreated"].ToString();
			} else if (exif.ContainsKey("CreateDate")) {
				Console.WriteLine(">> " + path + " > CreateDate");
				return exif["CreateDate"].ToString();
			} else return null;
		}
		public bool ContainsExif(string key) {
			return exif.ContainsKey(key);
		}


		/// <summary>
		/// Tests if the Image has a thumbnail saved on disk.
		/// </summary>
		/// <returns>Path to thumbnail or null</returns>
		public string HasThumbnail() {
			if ((thumbnail != null) && (File.Exists(thumbnail))) {
				return thumbnail;
			}
			return null;
		}

		/// <summary>
		/// Generates a thumbnail on the filesystem. Sets the i.v. thumbnail
		/// </summary>
		/// <param name="path">The FOLDER where the thumbnail will be created</param>
		/// <returns>Full path</returns>
		public System.Drawing.Image GenerateThumbnail(string path) {
			int smallside = 200, newWidth, newHeight;

			System.Drawing.Image.GetThumbnailImageAbort abort = delegate{
				Console.WriteLine("THUMBNAIL ABORTED!");
				return false;
			};
			IntPtr intptr = IntPtr.Zero;

			if (!File.Exists(this.path)) return null;
			Bitmap orig = new Bitmap(this.path);
			if (orig.Size.Height < orig.Size.Width) {
				newHeight = smallside;
				newWidth = orig.Size.Width * smallside / orig.Size.Height;
			} else {
				newWidth = smallside;
				newHeight = orig.Size.Height * smallside / orig.Size.Width;
			}
			System.Drawing.Image thumb = orig.GetThumbnailImage(newWidth,newHeight, abort, intptr);
			if (thumb == null) { throw new Exception("The thumbnail is null :S "); }
			return thumb;
		}




		#region Serialization
		/* Constructor for use with data returned from a BDB get. */
		public Image(byte[] buffer) {
			/* Fill in the fields from the buffer. */
			BinaryFormatter formatter = new BinaryFormatter();
			MemoryStream memStream = new MemoryStream(buffer);
			Image tmp = (Image)formatter.Deserialize(memStream);

			this.id = tmp.id;
			this.path = tmp.path;
			this.exif = tmp.exif;
			memStream.Close();
		}

		public Image Set(byte[] bytes) {
			return new Image(bytes);
		}

		/* 
		 * Marshall class data members into a single contiguous memory 
		 * location for the purpose of storing the data in a database.
		 */
		public byte[] GetBytes() {
			BinaryFormatter formatter = new BinaryFormatter();
			MemoryStream memStream = new MemoryStream();
			try {
				formatter.Serialize(memStream, this);
				byte[] bytes = memStream.GetBuffer();
				memStream.Close();
				return bytes;
			} catch {
				Console.WriteLine("ERRO A SERIALIZAR IMAGEM: " + this.ToString());
			}
			return null;
		}

		public byte[] Key() {
			return System.Text.Encoding.ASCII.GetBytes(this.id.ToString());
		}

		#endregion Serialization

		public string ToStringWithExif(string key) {
			return "Image " + this.id + " @ " + this.path + " - " + this.Exif(key);
		}
	}

	public class ImageExifComparer : IComparer<Image> {
		private string key;
		public ImageExifComparer(string key) {
			this.key = key;
		}

		public int Compare(Image x, Image y) {
			bool xok = x.ContainsExif(key);
			bool yok = y.ContainsExif(key);



			if (xok && yok)
				return x.Exif(key).ToString().CompareTo(y.Exif(key).ToString());
			else if (xok && !yok)
				return -1;
			else if (!xok && yok)
				return 1;
			else
				return 0;
		}
	}

	public class ImageDateComparer : IComparer<Image> {
		public int Compare(Image x, Image y) {
			return x.Exif("CreateDate").ToString().CompareTo(y.Exif("CreateDate").ToString());
		}
	}

	public class ImageIdComparer : IComparer<Image> {
		public int Compare(Image x, Image y) {
			return x.id.CompareTo(y.id);
		}
	}
}