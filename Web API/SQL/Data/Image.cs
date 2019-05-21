using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MySQLWrapper.Data
{
	class Image : SchemaItem
	{
		#region Schema Metadata
		public const string schema = "images";
		public static readonly ReadOnlyCollection<ColumnMetadata> metadata = Array.AsReadOnly(new ColumnMetadata[]
		{
			new ColumnMetadata("id", 50, MySqlDbType.VarChar),
			new ColumnMetadata("data", (int)Math.Pow(byte.MaxValue, 3) - 1, MySqlDbType.MediumBlob),
			new ColumnMetadata("extension", 10, MySqlDbType.VarChar),
 		});
		public static readonly ReadOnlyCollection<Index> indexes = Array.AsReadOnly(new Index[]
		{
			new Index("PRIMARY", Index.IndexType.PRIMARY, metadata[0])
		});
		private readonly object[] _fields = new object[metadata.Count];
		#endregion

		/// <summary>
		/// Array of image formats supported by android studio.
		/// </summary>
		public static readonly string[] ImageFormats = { ".jpeg", ".jpg", ".gif", ".bmp", ".png", ".webp", ".heif" };

		/// <summary>
		/// Creates a new instance of <see cref="Image"/>.
		/// </summary>
		/// <remarks>
		/// This constructor is intended for generic functions. Setting the fields
		/// should be done with the <see cref="Fields"/> property.
		/// </remarks>
		public Image() { }
		/// <summary>
		/// Creates a new instance of <see cref="Image"/> from an image file on this system.
		/// </summary>
		/// <param name="path">The path to an image file.</param>
		public Image(string path)
			: this(Path.GetFileNameWithoutExtension(path), File.ReadAllBytes(path), Path.GetExtension(path).ToLower())
		{ }
		/// <summary>
		/// Creates a new instance of <see cref="Image"/>.
		/// </summary>
		/// <param name="id">The unique identifier to give this image. File extensions are automatically excluded.</param>
		/// <param name="data">The raw byte data of the image file.</param>
		/// <param name="extension">The extension of the image. If null, it will attempt to extract it from <paramref name="id"/>.</param>
		public Image(string id, byte[] data, string extension = null)
		{
			Id = Path.GetFileNameWithoutExtension(id);
			Data = data;
			Extension = extension ?? Path.GetExtension(id);
		}

		#region Properties
		public string Id
		{
			get { return (string)Fields[0]; }
			set
			{
				if (value != null && value.Length > Metadata[0].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[0] = value;
			}
		}
		public byte[] Data
		{
			get { return (byte[])Fields[1]; }
			set
			{
				if (value != null && value.Length > Metadata[1].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				_fields[1] = value;
			}
		}
		public string Extension
		{
			get { return (string)Fields[2]; }
			set
			{
				if (value != null && value.Length > Metadata[2].Length)
					throw new ArgumentException("Value exceeds the maximum length specified in the metadata.");
				if (value != null && !ImageFormats.Contains(value.ToLower()))
					throw new FormatException($"Image format '{value}' is not supported.");
				_fields[2] = value;
			}
		}
		#endregion

		#region SchemaItem Support
		public override string Schema => schema;
		public override ReadOnlyCollection<ColumnMetadata> Metadata => metadata;
		public override ReadOnlyCollection<Index> Indexes => indexes;
		public override object[] Fields => _fields;
		#endregion

		#region Methods
		/// <summary>
		/// Selects columns based on the given conditions.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="columns">An array specifying which columns to return. Passing <c>null</c> will select all columns.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> filled with the results as object arrays.</returns>
		public static IEnumerable<object[]> Select(TechlabMySQL connection, string[] columns, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<Image>(connection, columns, condition, range);

		/// <summary>
		/// Selects all columns based on the given condition.
		/// </summary>
		/// <param name="connection">An opened <see cref="TechlabMySQL"/> object.</param>
		/// <param name="condition">A <see cref="MySqlConditionBuilder"/>. Passing <c>null</c> will select everything.</param>
		/// <param name="range">A nullable (ulong, ulong) tuple, specifying the range of results to return. Passing <c>null</c> will leave the range unspecified.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> containing instances of <see cref="Image"/>.</returns>
		public static IEnumerable<Image> Select(TechlabMySQL connection, MySqlConditionBuilder condition = null, (ulong Start, ulong Amount)? range = null)
			=> Select<Image>(connection, condition, range);
		#endregion
	}
}