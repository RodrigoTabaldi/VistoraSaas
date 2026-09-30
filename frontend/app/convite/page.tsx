import { InvitationAcceptance } from '../../components/invitation-acceptance';

export default async function InvitationPage({
  searchParams,
}: Readonly<{ searchParams: Promise<{ token?: string | string[] }> }>) {
  const params = await searchParams;
  const token = Array.isArray(params.token) ? params.token[0] ?? '' : params.token ?? '';
  return <InvitationAcceptance token={token} />;
}
