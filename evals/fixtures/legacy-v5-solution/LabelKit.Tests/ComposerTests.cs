using System;
using System.Linq;
using FluentAssertions;
using LabelKit.Tests.Assertions;
using TestSupport;
using Xunit;

namespace LabelKit.Tests
{
    public class ComposerTests
    {
        private static LabelComposer SampleLabel()
        {
            return new LabelComposer()
                .Add("INVOICE", bold: true)
                .NewLine()
                .Add("Item:", offset: 0)
                .Add("Widget", offset: 12)
                .NewLine()
                .Add("Total:", bold: true, offset: 0)
                .Add("19.99", bold: true, alignment: TokenAlignment.Right, offset: 12);
        }

        [Fact]
        public void Header_token_is_bold_and_chains_into_the_next_token()
        {
            var composer = SampleLabel();

            composer.Find(t => t.Text == "INVOICE")
                .Should().Exist()
                .And.BeBold()
                .And.BeAlignedLeft()
                .And.HaveOffset(0)
                .And.Subject.Next
                .Should().Exist()
                .And.BeAlignedLeft();
        }

        [Fact]
        public void Total_line_tokens_match_custom_predicates()
        {
            var composer = SampleLabel();

            composer.Find(t => t.Text == "Total:")
                .Should().Match(t => t.Line == 2)
                .And.BeBold();
        }

        [Fact]
        public void Tokens_keep_ascending_render_order()
        {
            var composer = SampleLabel();
            var tokens = composer.Tokens().ToList();

            tokens.Should().HaveCount(5);
            tokens.Count.Should().BeGreaterOrEqualTo(4);
            tokens.Count.Should().BeLessOrEqualTo(10);
            tokens.Should().BeInAscendingOrder(t => t.Line);
            tokens.Should().Match(list => list.All(t => t.Text.Length > 0));
        }

        [Fact]
        public void Boxed_token_can_be_inspected_through_As_chain()
        {
            var composer = SampleLabel();
            object boxed = composer.Find(t => t.Bold && t.Alignment == TokenAlignment.Right);

            boxed.As<LabelToken>().Text.Should().Be("19.99");
            boxed.Should().HavePropertyValue("Offset", 12);
            boxed.Should().HavePropertyValue("Bold", true, "the total amount is emphasized");
        }

        [Fact]
        public void Empty_text_is_rejected()
        {
            var composer = new LabelComposer();

            ((Action)(() => composer.Add("   "))).Should().Throw<ArgumentException>()
                .WithMessage("*visible text*");
            ((Action)(() => composer.Add("ok"))).Should().NotThrow();
        }

        [Fact]
        public void Missing_token_lookup_returns_null()
        {
            var composer = SampleLabel();

            composer.Find(t => t.Text == "DISCOUNT").Should().BeNull();
        }
    }
}
