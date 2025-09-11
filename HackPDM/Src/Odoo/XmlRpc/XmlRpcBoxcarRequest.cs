using System.Collections;

namespace HackPDM.Odoo.XmlRpc;

public class XmlRpcBoxcarRequest : XmlRpcRequest
{
	public IList Requests = new ArrayList();

	public override string MethodName => "system.multiCall";

	public override IList Params
	{
		get
		{
			_params.Clear();
			ArrayList arrayList = [];
			foreach ( XmlRpcRequest request in Requests )
			{
				Hashtable hashtable = new Hashtable
				{
					{ "methodName", request.MethodName },
					{ "params", request.Params }
				};
				arrayList.Add( hashtable );
			}

			_params.Add( arrayList );
			return _params;
		}
	}
}