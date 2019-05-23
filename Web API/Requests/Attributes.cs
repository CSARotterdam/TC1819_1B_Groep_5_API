using MySQLWrapper.Data;
using System;

namespace API.Requests {
	static partial class RequestMethodAttributes {
		internal sealed class skipTokenVerification : Attribute { };
		internal sealed class verifyPermission : Attribute {
			public User.UserPermission permission;
			public verifyPermission(User.UserPermission permission) {
				this.permission = permission;
			}
		};
	}
}
