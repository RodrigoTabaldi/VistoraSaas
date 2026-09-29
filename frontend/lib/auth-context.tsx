'use client';

import { createContext, useContext, type ReactNode } from 'react';

export type CurrentUser = {
  userId: string; name: string; email: string; role: string; organizationId: string;
};

const AuthContext = createContext<CurrentUser | null>(null);

export function AuthProvider({ user, children }: Readonly<{ user: CurrentUser; children: ReactNode }>) {
  return <AuthContext.Provider value={user}>{children}</AuthContext.Provider>;
}

export function useCurrentUser() {
  const user = useContext(AuthContext);
  if (!user) throw new Error('useCurrentUser must be used inside AuthProvider');
  return user;
}
