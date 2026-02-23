Rails.application.config.content_security_policy do |policy|
  policy.default_src :self
  policy.font_src    :self, "https://fonts.gstatic.com"
  policy.img_src     :self, :data
  policy.object_src  :none
  policy.script_src  :self, :unsafe_inline
  policy.style_src   :self, :unsafe_inline, "https://fonts.googleapis.com"
  policy.connect_src :self
end
