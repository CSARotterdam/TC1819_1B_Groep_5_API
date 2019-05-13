using MySQLWrapper.Data;
using System;
using System.Collections.Generic;
using System.Text;

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
