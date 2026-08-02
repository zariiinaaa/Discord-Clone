import ConversationDM from
  "@/components/islets/conversation-dm";

export default async function ConversationPage({
  params,
}: {
  params: Promise<{
    id: string;
  }>;
}) {
  const { id } = await params;
  const conversationId = Number(id);

  return (
    <ConversationDM
      conversationId={conversationId}
    />
  );
}