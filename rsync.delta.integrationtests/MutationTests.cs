using Xunit;

namespace Rsync.Delta.IntegrationTests
{
    public class MutationTests
    {
        [Fact]
        public void MutateFirstBlock()
        {
            // create test directory

            // write v1 file content (random source, fixed seed)
            // generate lib sig
            // generate rdiff sig
            // compare sigs

            // write v2 file content (apply mutation)
            // generate lib delta
            // generate rdiff delta
            // compare deltas

            // generate lib patched
            // compare to v2 file
        }
    }
}