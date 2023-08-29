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

namespace Softnet.Tracker.SiteModel
{
    class BiLinkedList<T> : IEnumerable<T> where T : class
    {
        BiLinked<T> m_First;
        BiLinked<T> m_Last;
        public BiLinkedList()
        {
            m_First = null;
            m_Last = null;
        }

        public bool IsEmpty
        {
            get { return m_First == null; }
        }

        public T GetFirst()
        {
            if (m_First != null) 
                return m_First.Data;
            return null;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(m_First);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public BiLinked<T> Add(T data)
        {
            BiLinked<T> newElement = new BiLinked<T>(data);
            if (m_Last != null)
            {
                m_Last.Next = newElement;
                newElement.Prev = m_Last;
                m_Last = newElement;
            }
            else
            {
                m_First = newElement;
                m_Last = newElement;
            }
            return newElement;
        }

        public void Remove(BiLinked<T> element)
        {
            if (element.Next != null)
            {
                if (element.Prev != null)
                {
                    element.Next.Prev = element.Prev;
                    element.Prev.Next = element.Next;
                    element.Next = null;
                    element.Prev = null;
                }
                else
                {
                    m_First = element.Next;
                    m_First.Prev = null;
                    element.Next = null;
                }
            }
            else if (element.Prev != null)
            {
                m_Last = element.Prev;
                m_Last.Next = null;
                element.Prev = null;
            }
            else
            {
                element.Next = null;
                element.Prev = null;
                m_First = null;
                m_Last = null;
            }
        }

        public class Enumerator : IEnumerator<T>, System.Collections.IEnumerator, IDisposable
        {
            BiLinked<T> m_Empty;
            BiLinked<T> m_Current;

            public Enumerator(BiLinked<T> first)
            {
                m_Empty = new BiLinked<T>(null);
                m_Empty.Next = first;
                m_Current = m_Empty;
            }

            Object System.Collections.IEnumerator.Current
            {
                get
                {
                    return m_Current.Data;
                }
            }

            T IEnumerator<T>.Current
            {
                get
                {
                    return m_Current.Data;
                }
            }

            public bool MoveNext()
            {
                if (m_Current.Next != null)
                {
                    m_Current = m_Current.Next;
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                m_Current = m_Empty;
            }

            void IDisposable.Dispose() { }
        }
    }
}
