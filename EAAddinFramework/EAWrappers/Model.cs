﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml;
using System.Linq;
using System.Windows.Forms;

using UML=TSF.UmlToolingFramework.UML;

namespace TSF.UmlToolingFramework.Wrappers.EA {
  public class Model : UML.UMLModel {
    private global::EA.Repository wrappedModel;

    /// Creates a model connecting to the first running instance of EA
    public Model(){
      object obj = Marshal.GetActiveObject("EA.App");
      global::EA.App eaApp = obj as global::EA.App;
      wrappedModel = eaApp.Repository;
    }

    /// constructor creates EAModel based on the given repository
    public Model(global::EA.Repository eaRepository){
      wrappedModel = eaRepository;
    }
    public UserControl addWindow(string title, string fullControlName)
    {
    	return this.wrappedModel.AddWindow(title,fullControlName) as UserControl;
    }
    /// the Element currently selected in EA
    public UML.Classes.Kernel.Element selectedElement {
      get {
        Object selectedItem;
        try
        {
            this.wrappedModel.GetContextItem(out selectedItem);
            return this.factory.createElement(selectedItem);
        }
        catch (COMException)
        {
            //something went wrong
            return null;
        }

        
      }
    	set
    	{
    		if (value is Package)
    		{
    			this.wrappedModel.ShowInProjectView(((Package)value).wrappedPackage);
    		}
    		else if (value is ElementWrapper)
    		{
    			this.wrappedModel.ShowInProjectView(((ElementWrapper)value).wrappedElement);
    		}
	        else if (value is Operation)
	        {
	            this.wrappedModel.ShowInProjectView(((Operation)value).wrappedOperation);
	        }
	        else if (value is Attribute)
	        {
	            this.wrappedModel.ShowInProjectView(((Attribute)value).wrappedAttribute);
	        }
	        else if (value is Parameter)
	        {
	        	Operation operation = (Operation)((Parameter)value).operation;
	        	this.wrappedModel.ShowInProjectView(operation.wrappedOperation);
	        }
    	}
    }
    
    /// returns the correct type of factory for this model
    public UML.UMLFactory factory {
      get { return Factory.getInstance(this); }
    }

    /// Finds the EA.Element with the given id and returns an EAElementwrapper 
    /// wrapping this element.
    public ElementWrapper getElementWrapperByID(int id){
      try{
        return this.factory.createElement
          (this.wrappedModel.GetElementByID(id)) as ElementWrapper;
      } catch( Exception )  {
        // element not found, return null
        return null;
      }
    }
    

    public UML.Classes.Kernel.Element getElementByGUID(string GUIDstring)
    {
    	UML.Classes.Kernel.Element foundElement = null;
    	//first try elementwrapper
    	foundElement = this.getElementWrapperByGUID(GUIDstring);
    	//then try Attribute
    	if (foundElement == null)
    	{
    		foundElement = this.getAttributeByGUID(GUIDstring);
    	}
    	//then try Operation
    	if (foundElement == null)
    	{
    		foundElement = this.getOperationByGUID(GUIDstring);
    	}
    	//then try ConnectorWrapper
    	if (foundElement == null)
    	{
    		foundElement = this.getRelationByGUID(GUIDstring);
    	}
    	//then try Parameter
    	if (foundElement == null)
    	{
    		foundElement = this.getParameterByGUID(GUIDstring);
    	}
    	return foundElement;
    }
    /// <summary>
    /// Finds the EA.Element with the given GUID and returns an EAElementwrapper 
    /// wrapping this element.
    /// </summary>
    /// <param name="GUID">the GUID of the element</param>
    /// <returns>the element with the given GUID</returns>
    public ElementWrapper getElementWrapperByGUID(string GUID){
      try{
        return this.factory.createElement
          (this.wrappedModel.GetElementByGuid(GUID)) as ElementWrapper;
      } catch( Exception )  {
        // element not found, return null
        return null;
      }
    }
    /// <summary>
    /// returns the elementwrappers that are identified by the Object_ID's returned by the given query
    /// </summary>
    /// <param name="sqlQuery">query returning the Object_ID's</param>
    /// <returns>elementwrappers returned by the query</returns>
    public List<ElementWrapper> getElementWrappersByQuery(string sqlQuery)
    {
      // get the nodes with the name "ObjectID"
      XmlDocument xmlObjectIDs = this.SQLQuery(sqlQuery);
      XmlNodeList objectIDNodes = xmlObjectIDs.SelectNodes("//Object_ID");
      List<ElementWrapper> elements = new List<ElementWrapper>();
      
      foreach( XmlNode objectIDNode in objectIDNodes ) 
      {
      	ElementWrapper element = this.getElementWrapperByID(int.Parse(objectIDNode.InnerText));
        if (element != null)
        {
        	elements.Add(element);
        }
      }
      return elements;
    }
    /// <summary>
    /// gets the Attribute with the given GUID
    /// </summary>
    /// <param name="GUID">the attribute's GUID</param>
    /// <returns>the Attribute with the given GUID</returns>
    public Attribute getAttributeByGUID (string GUID)
    {
    	try
    	{
    		return this.factory.createElement(this.wrappedModel.GetAttributeByGuid(GUID)) as Attribute;
    	}catch (Exception)
    	{
    		// attribute not found, return null
    		return null;
    	}
    	
    }
    /// <summary>
    /// gets the Attribute with the given ID
    /// </summary>
    /// <param name="GUID">the attribute's ID</param>
    /// <returns>the Attribute with the given ID</returns>
    public Attribute getAttributeByID (int attributID)
    {
    	try
    	{
    		return this.factory.createElement(this.wrappedModel.GetAttributeByID(attributID)) as Attribute;
    	}catch (Exception)
    	{
    		// attribute not found, return null
    		return null;
    	}
    	
    }
    /// <summary>
    /// gets the parameter by its GUID.
    /// This is a tricky one since EA doesn't provide a getParameterByGUID operation
    /// we have to first get the operation, then loop the pamarameters to find the one
    /// with the GUID
    /// </summary>
    /// <param name="GUID">the parameter's GUID</param>
    /// <returns>the Parameter with the given GUID</returns>
    public ParameterWrapper getParameterByGUID (string GUID)
    {
    	
    		//first need to get the operation for the parameter
    		string getOperationSQL = @"select p.OperationID from t_operationparams p
    									where p.ea_guid = '" + GUID +"'";
    		//first get the operation id
    		List<Operation> operations = this.getOperationsByQuery(getOperationSQL);
    		if (operations.Count > 0)
    		{
    			// the list of operations should only contain one operation
    			Operation operation = operations[0];
    			foreach ( ParameterWrapper parameter in operation.ownedParameters) {
    				if (parameter.ID == GUID) 
    				{
    					return parameter;
    				}
    			}
    		}
    	//parameter not found, return null
    	return null;
    }
    
    public UML.Diagrams.Diagram currentDiagram {
      get {
        return ((Factory)this.factory).createDiagram
          ( this.wrappedModel.GetCurrentDiagram() );
      }
      set { this.wrappedModel.OpenDiagram(((Diagram)value).DiagramID); }
    }
    
    internal Diagram getDiagramByID(int diagramID){
      return ((Factory)this.factory).createDiagram
        ( this.wrappedModel.GetDiagramByID(diagramID) ) as Diagram;
    }

    internal ConnectorWrapper getRelationByID(int relationID) {
      return ((Factory)this.factory).createElement
        ( this.wrappedModel.GetConnectorByID(relationID)) as ConnectorWrapper;
    }
	
    internal ConnectorWrapper getRelationByGUID(string relationGUID) {
      return ((Factory)this.factory).createElement
        ( this.wrappedModel.GetConnectorByGuid(relationGUID)) as ConnectorWrapper;
    }
    
    internal List<ConnectorWrapper> getRelationsByQuery(string SQLQuery){
      // get the nodes with the name "Connector_ID"
      XmlDocument xmlrelationIDs = this.SQLQuery(SQLQuery);
      XmlNodeList relationIDNodes = 
        xmlrelationIDs.SelectNodes("//Connector_ID");
      List<ConnectorWrapper> relations = new List<ConnectorWrapper>();
      foreach( XmlNode relationIDNode in relationIDNodes ) {
        int relationID;
        if (int.TryParse( relationIDNode.InnerText, out relationID)) {
          ConnectorWrapper relation = this.getRelationByID(relationID);
          relations.Add(relation);
        }
      }
      return relations;
    }
    
      internal List<Attribute> getAttributesByQuery(string SQLQuery){
      // get the nodes with the name "ea_guid"
      XmlDocument xmlAttributeIDs = this.SQLQuery(SQLQuery);
      XmlNodeList attributeIDNodes = xmlAttributeIDs.SelectNodes("//ea_guid");
      List<Attribute> attributes = new List<Attribute>();
      
      foreach( XmlNode attributeIDNode in attributeIDNodes ) 
      {
        Attribute attribute = this.getAttributeByGUID(attributeIDNode.InnerText);
        if (attribute != null)
        {
        	attributes.Add(attribute);
        }
      }
      return attributes;
    }
    internal List<Parameter>getParametersByQuery(string SQLQuery)
    {
      // get the nodes with the name "ea_guid"
      XmlDocument xmlParameterIDs = this.SQLQuery(SQLQuery);
      XmlNodeList parameterIDNodes = xmlParameterIDs.SelectNodes("//ea_guid");
      List<Parameter> parameters = new List<Parameter>();
      
      foreach( XmlNode parameterIDNode in parameterIDNodes ) 
      {
        Parameter parameter = this.getParameterByGUID(parameterIDNode.InnerText);
        if (parameter != null)
        {
        	parameters.Add(parameter);
        }
      }
      return parameters;
    }
    internal List<Operation>getOperationsByQuery(string SQLQuery)
    {
      // get the nodes with the name "OperationID"
      XmlDocument xmlOperationIDs = this.SQLQuery(SQLQuery);
      XmlNodeList operationIDNodes = xmlOperationIDs.SelectNodes("//OperationID");
      List<Operation> operations = new List<Operation>();
      
      foreach( XmlNode operationIDNode in operationIDNodes ) 
      {
      	int operationID;
      	if (int.TryParse(operationIDNode.InnerText,out operationID))
      	{
        	Operation operation = this.getOperationByID(operationID) as Operation;
      	    if (operation != null)
		    {
		       	operations.Add(operation);
		    }    
      	}
 
      }
      return operations;
    }

    /// generic query operation on the model.
    /// Returns results in an xml format
    public XmlDocument SQLQuery(string sqlQuery){
      XmlDocument results = new XmlDocument();
      results.LoadXml(this.wrappedModel.SQLQuery(sqlQuery));
      return results;
    }

    public void saveElement(UML.Classes.Kernel.Element element){
      ((Element)element).save();
    }

    public void saveDiagram(UML.Diagrams.Diagram diagram){
      throw new NotImplementedException();
    }

    internal UML.Classes.Kernel.Element getElementWrapperByPackageID
      (int packageID)
    {
      return this.factory.createElement
        ( this.wrappedModel.GetPackageByID(packageID).Element );
    }

    //returns a list of diagrams according to the given query.
    //the given query should return a list of diagram id's
    internal List<Diagram> getDiagramsByQuery(string sqlGetDiagrams)
    {
        // get the nodes with the name "Diagram_ID"
        XmlDocument xmlDiagramIDs = this.SQLQuery(sqlGetDiagrams);
        XmlNodeList diagramIDNodes =
          xmlDiagramIDs.SelectNodes("//Diagram_ID");
        List<Diagram> diagrams = new List<Diagram>();
        foreach (XmlNode diagramIDNode in diagramIDNodes)
        {
            int diagramID;
            if (int.TryParse(diagramIDNode.InnerText, out diagramID))
            {
                Diagram diagram = this.getDiagramByID(diagramID);
                diagrams.Add(diagram);
            }
        }
        return diagrams;
    }

    internal UML.Classes.Kernel.Operation getOperationByGUID(string guid)
    {
        return this.factory.createElement(this.wrappedModel.GetMethodByGuid(guid)) as UML.Classes.Kernel.Operation;
    }
    internal UML.Classes.Kernel.Operation getOperationByID(int operationID)
    {
        return this.factory.createElement(this.wrappedModel.GetMethodByID(operationID)) as UML.Classes.Kernel.Operation;
    }

    
    internal void executeSQL(string SQLString)
    {
    	this.wrappedModel.Execute(SQLString);
    }
  	
	public void selectDiagram(Diagram diagram)
	{
		this.wrappedModel.ShowInProjectView(diagram.wrappedDiagram);
	}
  	
	public UML.UMLItem getItemFromFQN(string FQN)
	{
		//split the FQN in the different parts
		UML.UMLItem foundItem = null;
		foreach(UML.Classes.Kernel.Package package in  this.rootPackages)
		{
			
			foundItem = package.getItemFromRelativePath(FQN.Split('.').ToList<string>());
			if (foundItem != null)
			{
				break;
			}
		}
		return foundItem;
	}
  	
	public  HashSet<UML.Classes.Kernel.Package> rootPackages {
		get 
		{
			
			return new HashSet<UML.Classes.Kernel.Package>(this.factory.createElements(this.wrappedModel.Models).Cast<UML.Classes.Kernel.Package>());
		}
	}
  	
	public  UML.Diagrams.Diagram selectedDiagram
	{
		get
		{
			object item ;
			this.wrappedModel.GetTreeSelectedItem(out item);
			global::EA.Diagram diagram = item as global::EA.Diagram;
			if (diagram != null)
			{
				return this.factory.createDiagram(diagram);
			}
			else
			{
				return null;
			}
		}
		set
		{
			value.select();
			
		}
	}
	public  TSF.UmlToolingFramework.UML.UMLItem selectedItem {
		get {
		 	UML.UMLItem item = this.selectedElement;
		 	if (item == null)
		 	{
		 		item = this.selectedDiagram;
		 	}
		 	return item;
		}
		set 
		{
			if (value is UML.Diagrams.Diagram)
			{
				this.selectedDiagram = value as UML.Diagrams.Diagram;
			}
			else if (value is UML.Classes.Kernel.Element)
			{
				this.selectedElement = value as UML.Classes.Kernel.Element;
			}
		}
	}
  }
}
