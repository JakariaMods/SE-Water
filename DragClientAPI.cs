using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.ModAPI;

namespace Draygo.Drag.API
{
	public class DragClientAPI
	{
		private Func<IMyEntity, object> MessageFactory;
		private Action<object, int, object> MessageSetter;
		private Func<object, int, object> MessageGetter;
		static DragClientAPI instance;
		bool m_Heartbeat = false;
		private const long REGISTRATIONID = 571920453;

		public bool Heartbeat
		{
			get
			{
				return m_Heartbeat;
			}
		}

		public DragClientAPI()
		{
			instance = this;
		}

		public void Init()
		{
			MyAPIGateway.Utilities.RegisterMessageHandler(REGISTRATIONID, RegisterComponents);
		}

		private void RegisterComponents(object obj)
		{
			if (m_Heartbeat)
				return;
			if(obj is MyTuple<Func<IMyEntity, object>, Action<object, int, object>, Func<object, int, object>>)
			{
				var handles = (MyTuple<Func<IMyEntity, object>, Action<object, int, object>, Func<object, int, object>>)obj;
				MessageFactory = handles.Item1;
				MessageSetter = handles.Item2;
				MessageGetter = handles.Item3;
				m_Heartbeat = true;
            }
		}

		public void Close()
		{
			MyAPIGateway.Utilities.UnregisterMessageHandler(REGISTRATIONID, RegisterComponents);
		}

		#region intercom
		private object Factory(IMyEntity entityObject)
		{
			return MessageFactory(entityObject);
		}
		private object MessageGet(object BackingObject, int Member)
		{
			return MessageGetter(BackingObject, Member);
		}
		private void MessageSet(object BackingObject, int Member, object Value)
		{
			MessageSetter(BackingObject, Member, Value);
		}

		#endregion

		private enum DragMembers : int
		{
			Viscosity = 0
		}

		public class DragObject
		{
			private object BackingObject;
			private IMyEntity m_Entity;
			public float ViscosityMultiplier
			{
				get
				{
					if(instance == null || !instance.Heartbeat)
						return 1f;
					if(BackingObject == null )
					{
						BackingObject = instance.Factory(m_Entity);
					}
					if (BackingObject != null)
						return (float)instance.MessageGet(BackingObject, (int)DragMembers.Viscosity);
					else
						return 1f;
                }
				set
				{
					if (instance == null || !instance.Heartbeat)
						return;
					if (BackingObject == null)
					{
						BackingObject = instance.Factory(m_Entity);
					}
					if(BackingObject != null)
					{
						instance.MessageSet(BackingObject, (int)DragMembers.Viscosity, value);
					}
				}
			}

			public DragObject(IMyEntity Ent)
			{
				m_Entity = Ent;
				if(instance != null	&& instance.MessageFactory != null)
				{
					BackingObject = instance.MessageFactory(Ent);
                }
            }

			public static DragObject Factory(IMyEntity Ent)
			{
				return new DragObject(Ent);
			}
		}
	}
}
