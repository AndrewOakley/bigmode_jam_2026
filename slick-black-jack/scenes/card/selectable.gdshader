shader_type canvas_item;

uniform vec4 pulse_color : source_color = vec4(1.0, 0.9, 0.0, 1.0);
uniform float speed : hint_range(0.0, 5.0) = 2.5;
uniform float min_intensity : hint_range(0.0, 1.0) = 0.0;
uniform float max_intensity : hint_range(0.0, 1.0) = 0.4;

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);

    // Create a smooth sine wave pulse between 0 and 1
    float pulse = (sin(TIME * speed) + 1.0) * 0.5;

    // Map that pulse to our min/max intensity range
    float intensity = mix(min_intensity, max_intensity, pulse);

    // Mix the original color with the pulse color
    // We multiply by tex_color.a to ensure the pulse doesn't show on transparent areas
    vec3 final_color = mix(tex_color.rgb, pulse_color.rgb, intensity);

    COLOR = vec4(final_color, tex_color.a);
}