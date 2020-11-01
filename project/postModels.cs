using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace csgo.postModels
{
	public class Login
	{

		[Required]
		[DataType(DataType.Text)]
		[Display(Name = "username")]
		public string username { get; set; }

		[Required]
		[DataType(DataType.Password)]
		[Display(Name = "password")]
		public string password { get; set; }
	}
	public class sellerRequest
	{

		[Required]
		[DataType(DataType.Text)]
		[Display(Name = "age")]
		public string age { get; set; }

		[Required]
		[DataType(DataType.Text)]
		[Display(Name = "nationality")]
		public string nationality { get; set; }

		[Required]
		[DataType(DataType.Text)]
		[Display(Name = "description")]
		public string description { get; set; }
		[Required]
		[DataType(DataType.PhoneNumber)]
		[Display(Name = "phoneNumber")]
		public string phoneNumber { get; set; }
		[Required]
		[DataType(DataType.Text)]
		[Display(Name = "question")]
		public string question { get; set; }
	}
	public class recoveryPassword
	{


		[Required]
		[DataType( DataType.Password )]
		[Display( Name = "password" )]
		public string password { get; set; }
		[Required]
		[DataType( DataType.Password )]
		[Display( Name = "passwordre" )]
		public string passwordre { get; set; }
	}
	public class forgotPassword
	{

		[Required]
		[DataType( DataType.EmailAddress )]
		[Display( Name = "email" )]
		public string email { get; set; }

	}
	public class twoFactor
	{

		[Required]
		[DataType(DataType.Text)]
		[Display(Name = "code")]
		public string code { get; set; }

	}
	public class Register
	{

		[Required]
		[DataType( DataType.Text )]
		[Display( Name = "username" )]
		public string username { get; set; }

		[Required]
		[DataType( DataType.Password )]
		[Display( Name = "password" )]
		public string password { get; set; }

		[Required]
		[DataType( DataType.EmailAddress )]
		[Display( Name = "email" )]
		public string email { get; set; }
	}
	public class addAccount
	{

		[Required]
		[DataType(DataType.Text)]
		[Display(Name = "username")]
		public string username { get; set; }

		[Required]
		[DataType(DataType.Password)]
		[Display(Name = "password")]
		public string password { get; set; }

		[Required]
		[DataType(DataType.EmailAddress)]
		[Display(Name = "email")]
		public string email { get; set; }

		[Required]
		[DataType(DataType.Text)]
		[Display(Name = "rank")]
		public string rank { get; set; }

		[Required]
		[DataType(DataType.Text)]
		[Display(Name = "hours")]
		public string hours { get; set; }

		[Required]
		[Range(typeof(bool), "false", "true", ErrorMessage = "The field Is Active must be checked.")]
		[Display(Name = "prime")]
		public bool prime { get; set; }

		[Required]
		[DataType(DataType.Text)]
		[Display(Name = "wins")]
		public string wins { get; set; }

		[Required]
		[DataType(DataType.Text)]
		[Display(Name = "steamlink")]
		public string steamLink { get; set; }

		[Required]
		[DataType(DataType.Upload)]
		[Display(Name = "image")]
		public string imageUrl { get; set; }


	}
	
}