namespace EntityFramework.Functions.Tests.Library.Examples
{
    using System.ComponentModel.DataAnnotations.Schema;

    [ComplexType]
    public class ContactInformation
    {
        public int PersonID { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string JobTitle { get; set; }

        public string BusinessEntityType { get; set; }
    }
}
