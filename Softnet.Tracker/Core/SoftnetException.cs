/*
*   Copyright 2023 Robert Koifman
*   
*   Licensed under the Apache License, Version 2.0 (the "License");
*   you may not use this file except in compliance with the License.
*   You may obtain a copy of the License at
*
*   http://www.apache.org/licenses/LICENSE-2.0
*
*   Unless required by applicable law or agreed to in writing, software
*   distributed under the License is distributed on an "AS IS" BASIS,
*   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*   See the License for the specific language governing permissions and
*   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Softnet.Tracker.Core
{
    public class SoftnetException : Exception
    {
        int m_ErrorCode;
        public int ErrorCode
        {
            get { return m_ErrorCode; }
        }

        public SoftnetException(int errorCode)
        {
            m_ErrorCode = errorCode;
        }

        public SoftnetException(int errorCode, string message) 
            : base(message)
        {
            m_ErrorCode = errorCode;
        }

        public SoftnetException(System.Data.SqlClient.SqlException ex)
            : base(ex.Message)
        {
            m_ErrorCode = ErrorCodes.DBMS_ERROR;
        }

        public SoftnetException(System.Configuration.ConfigurationErrorsException ex)
            : base(ex.Message)
        {
            m_ErrorCode = ErrorCodes.CONFIG_ERROR;
        }
    }
}
