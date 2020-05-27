/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Aliyun.Storages
{
	public class StorageClient
	{
		#region 成员字段
		private HttpClient _http;
		private ICertificate _certificate;
		private StorageServiceCenter _serviceCenter;
		#endregion

		#region 构造函数
		internal StorageClient(StorageServiceCenter serviceCenter, ICertificate certificate)
		{
			_serviceCenter = serviceCenter ?? throw new ArgumentNullException(nameof(serviceCenter));
			_certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
			_http = new HttpClient(new HttpClientHandler(certificate, StorageAuthenticator.Instance));
		}
		#endregion

		#region 公共属性
		public ICertificate Certificate
		{
			get
			{
				return _certificate;
			}
		}

		public StorageServiceCenter ServiceCenter
		{
			get
			{
				return _serviceCenter;
			}
		}
		#endregion

		#region 内部属性
		internal HttpClient HttpClient
		{
			get
			{
				return _http;
			}
		}
		#endregion

		#region 公共方法
		public StorageUploader GetUploader(string path)
		{
			return new StorageUploader(this, path);
		}

		public StorageUploader GetUploader(string path, int bufferSize)
		{
			return new StorageUploader(this, path, bufferSize);
		}

		public StorageUploader GetUploader(string path, IDictionary<string, object> extendedProperties)
		{
			return new StorageUploader(this, path, extendedProperties);
		}

		public StorageUploader GetUploader(string path, IDictionary<string, object> extendedProperties, int bufferSize)
		{
			return new StorageUploader(this, path, extendedProperties, bufferSize);
		}

		public async Task<bool> CopyAsync(string source, string destination)
		{
			var request = new HttpRequestMessage(HttpMethod.Put, _serviceCenter.GetRequestUrl(destination));

			request.Headers.Add(StorageHeaders.OSS_COPY_SOURCE, source);

			var result = await _http.SendAsync(request);
			return result.IsSuccessStatusCode;
		}

		public async Task<bool> DeleteAsync(string path)
		{
			return (await _http.DeleteAsync(_serviceCenter.GetRequestUrl(path))).IsSuccessStatusCode;
		}

		public async Task<Stream> DownloadAsync(string path, IDictionary<string, object> properties = null)
		{
			var response = await _http.GetAsync(_serviceCenter.GetRequestUrl(path));

			//确认返回是否是成功
			response.EnsureSuccessStatusCode();

			if(properties != null)
				this.FillProperties(response, properties);

			return await response.Content.ReadAsStreamAsync();
		}

		public Task<bool> CreateAsync(string path, IDictionary<string, object> extendedProperties = null)
		{
			return this.CreateAsync(path, null, extendedProperties);
		}

		public async Task<bool> CreateAsync(string path, Stream stream, IDictionary<string, object> extendedProperties = null)
		{
			var request = this.CreateHttpRequest(HttpMethod.Put, path, this.EnsureCreatedTime(extendedProperties));

			if(stream != null)
				request.Content = new StreamContent(stream);

			return (await _http.SendAsync(request)).IsSuccessStatusCode;
		}

		public async Task<bool> ExistsAsync(string path)
		{
			var request = new HttpRequestMessage(HttpMethod.Head, _serviceCenter.GetRequestUrl(path));

			var result = await _http.SendAsync(request);
			return result.IsSuccessStatusCode;
		}

		public async Task<IDictionary<string, object>> GetExtendedPropertiesAsync(string path)
		{
			var request = new HttpRequestMessage(HttpMethod.Head, _serviceCenter.GetRequestUrl(path));
			var response = await _http.SendAsync(request);

			//确认返回是否是成功
			response.EnsureSuccessStatusCode();

			var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

			this.FillProperties(response, result);

			return result;
		}

		public async Task<bool> SetExtendedPropertiesAsync(string path, IDictionary<string, object> extendedProperties)
		{
			if(extendedProperties == null || extendedProperties.Count < 1)
				return false;

			var properties = await this.GetExtendedPropertiesAsync(path);

			foreach(var property in extendedProperties)
			{
				properties[property.Key] = property.Value;
			}

			var request = this.CreateHttpRequest(HttpMethod.Put, path, properties);
			request.Headers.Add(StorageHeaders.OSS_COPY_SOURCE, path);

			return (await _http.SendAsync(request)).IsSuccessStatusCode;
		}

		public async Task<StorageSearchResult> SearchAsync(string path, Func<string, string> getUrl)
		{
			if(!string.IsNullOrWhiteSpace(path))
			{
				if(path.Contains('?'))
					throw new ArgumentException(string.Format("The '{0}' pattern contains invalid characters.", path));

				var index = path.IndexOf('*');

				if(index >= 0 && index < path.Length - 1)
					throw new ArgumentException(string.Format("The '*' character at last only in the '{0}' pattern.", path));

				path = path.Trim('*');
			}

			string baseName, resourcePath;
			var url = _serviceCenter.GetBaseUrl(path, out baseName, out resourcePath) + "?prefix=" + Uri.EscapeDataString(resourcePath) + "&delimiter=%2F&max-keys=21";
			var response = await _http.GetAsync(url);

			//确保返回的内容是成功
			response.EnsureSuccessStatusCode();

			var resolver = new StorageSearchResultResolver(_serviceCenter, _http, getUrl);
			return await resolver.Resolve(response);
		}
		#endregion

		#region 内部方法
		internal HttpRequestMessage CreateHttpRequest(HttpMethod method, string path, IDictionary<string, object> extendedProperties = null)
		{
			var request = new HttpRequestMessage(method, _serviceCenter.GetRequestUrl(path));

			if(extendedProperties != null && extendedProperties.Count > 0)
			{
				foreach(var entry in extendedProperties)
				{
					if(string.IsNullOrWhiteSpace(entry.Key) || entry.Key.Contains(':'))
						continue;

					request.Headers.Add(StorageHeaders.OSS_META + entry.Key.Trim().ToLowerInvariant(), entry.Value == null ? string.Empty : Uri.EscapeDataString(entry.Value.ToString()));
				}
			}

			return request;
		}

		internal IDictionary<string, object> EnsureCreatedTime(IDictionary<string, object> properties)
		{
			if(properties == null)
			{
				properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
				properties[StorageHeaders.ZFS_CREATEDTIME_PROPERTY] = Utility.GetGmtTime();
			}
			else
			{
				object value;

				if(!properties.TryGetValue(StorageHeaders.ZFS_CREATEDTIME_PROPERTY, out value))
					properties[StorageHeaders.ZFS_CREATEDTIME_PROPERTY] = Utility.GetGmtTime();
			}

			return properties;
		}
		#endregion

		#region 私有方法
		private void FillProperties(HttpResponseMessage response, IDictionary<string, object> properties)
		{
			if(response == null || !response.IsSuccessStatusCode || properties == null)
				return;

			if(response.Content != null && response.Content.Headers.LastModified.HasValue)
				properties[StorageHeaders.HTTP_LAST_MODIFIED_PROPERTY] = response.Content.Headers.LastModified.Value.ToString();

			if(response.Content != null && response.Content.Headers.ContentLength.HasValue)
				properties[StorageHeaders.HTTP_CONTENT_LENGTH_PROPERTY] = response.Content.Headers.ContentLength.Value.ToString();

			if(response.Headers.ETag != null && !string.IsNullOrWhiteSpace(response.Headers.ETag.Tag))
				properties[StorageHeaders.HTTP_ETAG_PROPERTY] = response.Headers.ETag.Tag.Trim('"');

			foreach(var header in response.Headers)
			{
				if(header.Key.Length > StorageHeaders.OSS_META.Length && header.Key.StartsWith(StorageHeaders.OSS_META))
				{
					var key = header.Key.Substring(StorageHeaders.OSS_META.Length);

					if(key.Length > 0)
						properties[key] = Uri.UnescapeDataString(string.Join("", header.Value));
				}
			}
		}
		#endregion
	}
}
