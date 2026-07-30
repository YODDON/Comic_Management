export const getRoleLabel = (role) => role === 'Reader' ? 'User' : role

export const formatRoleLabels = (roles, fallback = 'User') => {
  if (!roles?.length) return fallback
  return roles.map(getRoleLabel).join(', ')
}
