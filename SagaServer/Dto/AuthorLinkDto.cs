using System;

namespace SagaServer.Dto
{
    public class AuthorLinkDto : IEquatable<AuthorLinkDto>
    {
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }

        bool IEquatable<AuthorLinkDto>.Equals(AuthorLinkDto other)
        {
            var _result = this.AuthorId.Equals(other.AuthorId) && this.AuthorName.Equals(other.AuthorName);
            return _result;
        }

        public override int GetHashCode()
        {
            int hashProductName = AuthorName == null ? 0 : AuthorName.GetHashCode();
            int hashProductLink = AuthorId == null ? 0 : AuthorId.GetHashCode();

            //Calculate the hash code for the product.
            return hashProductName ^ hashProductLink;
        }
    }
}