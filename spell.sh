pushd $(git rev-parse --show-toplevel)
perl -e '
my @expect_files=qw('".github/actions/spelling/expect.txt"');
@ARGV=@expect_files;
my @stale=qw('"MUL STx "');
my $re=join "|", @stale;
my $suffix=".".time();
my $previous="";
sub maybe_unlink { unlink($_[0]) if $_[0]; }
while (<>) {
  if ($ARGV ne $old_argv) { maybe_unlink($previous); $previous="$ARGV$suffix"; rename($ARGV, $previous); open(ARGV_OUT, ">$ARGV"); select(ARGV_OUT); $old_argv = $ARGV; }
  next if /^(?:$re)(?:(?:\r|\n)*$| .*)/; print;
}; maybe_unlink($previous);'
perl -e '
my $new_expect_file=".github/actions/spelling/expect.txt";
use File::Path qw(make_path);
make_path ".github/actions/spelling";
open FILE, q{<}, $new_expect_file; chomp(my @words = <FILE>); close FILE;
my @add=qw('"aspera ata autogen backend Consts FDC Fusce gota hashlink idi ishtar LDSF llvm mul pctor pregen STF STSF stx "');
my %items; @items{@words} = @words x (1); @items{@add} = @add x (1);
@words = sort {lc($a) cmp lc($b)} keys %items;
open FILE, q{>}, $new_expect_file; for my $word (@words) { print FILE "$word\n" if $word =~ /\w/; };
close FILE;'
popd