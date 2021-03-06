pushd $(git rev-parse --show-toplevel)
perl -e '
my $new_expect_file=".github/actions/spelling/expect.txt";
use File::Path qw(make_path);
make_path ".github/actions/spelling";
open FILE, q{<}, $new_expect_file; chomp(my @words = <FILE>); close FILE;
my @add=qw('"waproj "');
my %items; @items{@words} = @words x (1); @items{@add} = @add x (1);
@words = sort {lc($a) cmp lc($b)} keys %items;
open FILE, q{>}, $new_expect_file; for my $word (@words) { print FILE "$word\n" if $word =~ /\w/; };
close FILE;'
popd