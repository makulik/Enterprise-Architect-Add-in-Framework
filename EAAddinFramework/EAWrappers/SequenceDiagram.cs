﻿using System;
using System.Collections.Generic;
using System.Linq;

using UML=TSF.UmlToolingFramework.UML;

namespace TSF.UmlToolingFramework.Wrappers.EA 
{
	/// <summary>
	/// Description of SequenceDiagram.
	/// </summary>
	public class SequenceDiagram:Diagram,UML.Diagrams.SequenceDiagram
	{
		public SequenceDiagram(Model model, global::EA.Diagram wrappedDiagram ):base(model,wrappedDiagram)
		{
		}
		
		/// <summary>
		/// returns all operations called in this sequence diagram
		/// </summary>
		/// <returns>all operations called in this sequence diagram</returns>
		public List<UML.Classes.Kernel.Operation> getCalledOperations()
		{
			List<UML.Classes.Kernel.Operation> calledOperations = new List<UML.Classes.Kernel.Operation>();
			foreach ( DiagramLinkWrapper linkwrapper in this.diagramLinkWrappers) 
			{
				Message message = linkwrapper.relation as Message;
				if (message != null)
				{
					UML.Classes.Kernel.Operation operation = message.calledOperation;
					if (operation != null)
					{
						calledOperations.Add(operation);
					}
				}
			}
			return calledOperations;
		}
		/// <summary>
		/// gets all relations that are specific to this sequence diagram.
		/// </summary>
		/// <returns>all messages and other relations of the diagram</returns>
		internal override List<ConnectorWrapper> getRelations()
        {
            string SQLQuery = @"SELECT c.Connector_ID
                                FROM  t_Connector c 
                                WHERE c.DiagramID = "
                            + this.wrappedDiagram.DiagramID + " order by c.SeqNo;";
            return this.model.getRelationsByQuery(SQLQuery);
        } 
		
	}
}
