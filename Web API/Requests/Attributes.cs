using MySQLWrapper.Data;
using Newtonsoft.Json;
using System;

namespace API.Requests {
	static partial class RequestMethodAttributes {
		internal class RequestAttribute : Attribute { };
		internal sealed class IgnoreUserToken : RequestAttribute { };
		internal sealed class RequiresPermissionLevel : RequestAttribute
		{
			public UserPermission Permission;
			public RequiresPermissionLevel(UserPermission permission) {
				Permission = permission;
			}
		};
	}
}
